/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Diagnostics.Contracts;
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    /// <summary>
    /// NOTES:
    /// 1. Parsing is done in the order as the properties are declares in the source.
    /// 2. If the first property implements IConceptInfo, it is possible to embed the concept within that parent concept.
    /// ("module x { entity y; }") or to use it explicitly ("entity x.y;").
    /// 3. If the first property type is IConceptInfo interface, not the implementation, it can reference any concept
    /// but can be used only in the embedded form.
    /// 4. Recursive "parent" property (referencing the same concept type) does not have to be the first property
	/// to be used in the embedded form. This allows construction of recursive concepts such as menus.
    /// </summary>
    public class GenericParser : IConceptParser
    {
        private readonly string Keyword;
        private readonly ConceptMember[] Members;
        private readonly Type ConceptInfoType;

        public GenericParser(Type conceptInfoType, string keyword)
        {
            Contract.Requires(conceptInfoType != null);
            Contract.Requires(keyword != null);

            this.ConceptInfoType = conceptInfoType;
            this.Keyword = keyword;
            Members = ConceptMembers.Get(conceptInfoType).ToArray();
        }

        public virtual ValueOrError<IConceptInfo> Parse(ITokenReader tokenReader, Stack<IConceptInfo> context)
        {
            if (tokenReader.TryRead(Keyword))
                return ParseMembers(tokenReader, context.Count > 0 ? context.Peek() : null, false);
            return ValueOrError<IConceptInfo>.CreateError("");
        }

        public event DslParser.OnMemberReadEvent OnMemberRead;
        private ValueOrError<IConceptInfo> ParseMembers(ITokenReader tokenReader, IConceptInfo lastConcept, bool readingAReference)
        {
            IConceptInfo conceptInfo = (IConceptInfo)Activator.CreateInstance(ConceptInfoType);
            bool firstMember = true;
            bool lastPropertyWasInlineParent = false;
            bool lastConceptUsed = false;

            var listOfMembers = readingAReference ? Members.Where(m => m.IsKey) : Members.Where(m => m.IsParsable);
            foreach (ConceptMember member in listOfMembers)
            {
                var valueOrError = ReadMemberValue(member, tokenReader, lastConcept, firstMember, ref lastPropertyWasInlineParent, ref lastConceptUsed, readingAReference);
                OnMemberRead?.Invoke(tokenReader, ConceptInfoType, member, valueOrError);

                if (valueOrError.IsError)
                    return ValueOrError<IConceptInfo>.CreateError(string.Format(CultureInfo.InvariantCulture,
                        "Cannot read the value of {0} in {1}. {2}",
                        member.Name, ConceptInfoType.Name, valueOrError.Error));

                member.SetMemberValue(conceptInfo, valueOrError.Value);
                firstMember = false;
            }

            if (!lastConceptUsed && lastConcept != null)
                return ValueOrError<IConceptInfo>.CreateError(string.Format(
                    "This concept cannot be enclosed within {0}. Trying to read {1}.",
                    lastConcept.GetType().Name, ConceptInfoType.Name));

            return ValueOrError<IConceptInfo>.CreateValue(conceptInfo);
        }

        public ValueOrError<object> ReadMemberValue(ConceptMember member, ITokenReader tokenReader, IConceptInfo lastConcept,
            bool firstMember, ref bool lastPropertyWasInlineParent, ref bool lastConceptUsed, bool readingAReference)
        {
            try
            {
                if (lastPropertyWasInlineParent && member.IsKey && !member.IsConceptInfo) // TODO: Removing "IsConceptInfo" from this condition would produce a mismatch. Think of a better solution for parsing the concept key.
                {
                    if (!tokenReader.TryRead("."))
                        return ValueOrError<object>.CreateError(string.Format(
                            "Parent property and the following key value ({0}) must be separated with a dot. Expected \".\"",
                            member.Name));
                }
                lastPropertyWasInlineParent = false;

                if (member.IsStringType)
                    return tokenReader.ReadText().ChangeType<object>();

                if (member.ValueType == typeof(IConceptInfo))
                    if (firstMember && lastConcept != null)
                    {
                        lastConceptUsed = true;
                        return (object)lastConcept;
                    }
                    else
                        return ValueOrError<object>.CreateError("Member of type IConceptInfo can only be used as a first member and enclosed within the referenced parent concept.");

                if (member.IsConceptInfo && lastConcept != null && member.ValueType.IsInstanceOfType(lastConcept)
                         && member.ValueType.IsAssignableFrom(ConceptInfoType)) // Recursive "parent" property
                {
                    lastConceptUsed = true;
                    return (object)lastConcept;
                }

                if (member.IsConceptInfo && lastConcept != null && member.ValueType.IsInstanceOfType(lastConcept)
                         && firstMember)
                {
                    lastConceptUsed = true;
                    return (object)lastConcept;
                }

                if (member.IsConceptInfo && firstMember)
                {
                    if (member.ValueType == ConceptInfoType)
                        return ValueOrError.CreateError(string.Format(
                            "Recursive concept {0} cannot be used as a root because its parent property ({1}) must reference another concept. Use a non-recursive concept for the root and a derivation of the root concept with additional parent property as a recursive concept.",
                            ConceptInfoHelper.GetKeywordOrTypeName(ConceptInfoType), member.Name));

                    if (!readingAReference && Members.Where(m => m.IsParsable).Count() == 1)
                    {
                        // This validation is not necessary for consistent parsing. It is enforced simply to avoid ambiguity when parsing
                        // similar concepts such as "Logging { AllProperties; }", "History { AllProperties; }" and "Persisted { AllProperties; }".

                        var parentMembers = ConceptMembers.Get(member.ValueType).Where(m => m.IsParsable).ToArray();
                        if (parentMembers.Count() == 1 && parentMembers.Single().IsConceptInfo)
                            return ValueOrError.CreateError(string.Format(
                                "{0} must be enclosed within the referenced parent concept {1}. A single-reference concept that references another single-reference concept must always be used with embedded syntax to avoid ambiguity.",
                                ConceptInfoHelper.GetKeywordOrTypeName(ConceptInfoType),
                                ConceptInfoHelper.GetKeywordOrTypeName(member.ValueType)));
                    }

                    GenericParser subParser = new GenericParser(member.ValueType, "");
                    subParser.OnMemberRead += OnMemberRead;
                    lastConceptUsed = true;
                    lastPropertyWasInlineParent = true;
                    return subParser.ParseMembers(tokenReader, lastConcept, true).ChangeType<object>();
                }

                if (member.IsConceptInfo)
                {
                    GenericParser subParser = new GenericParser(member.ValueType, "");
                    subParser.OnMemberRead += OnMemberRead;
                    return subParser.ParseMembers(tokenReader, null, true).ChangeType<object>();
                }

                return ValueOrError.CreateError(string.Format(
                    "GenericParser does not support members of type \"{0}\". Try using string or implementation of IConceptInfo.",
                    member.ValueType.Name));
            }
            catch (DslSyntaxException ex)
            {
                return ValueOrError<object>.CreateError(ex.Message);
            }
        }
    }
}