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
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    /// <summary>
    /// NOTES:
    /// 1. Parsing is done in the order as the properties are declares in the source.
    /// 2. It is possible to nest a concept within a parent concept ("module x { entity y; }") or to use it explicitly ("entity x.y;").
    /// Parent reference is defined by a property that references another concept, marked with <see cref="ConceptParentAttribute"/> or the first property by default.
    /// Derived concept can override parent property with its own property marked with <see cref="ConceptParentAttribute"/>;
    /// this allows construction of recursive concepts such as menu items.
    /// 3. If the parent property type is IConceptInfo interface, not the implementation, it can reference any concept
    /// but can only be used in the nested form.
    /// 4. Recursive "parent" property (referencing the same concept type), marked with <see cref="ConceptParentAttribute"/>
    /// does not have to be the first property to be used in the nested form.
    /// </summary>
    public class GenericParser : IConceptParser
    {
        private readonly string Keyword;
        private readonly ConceptMember[] Members;
        private readonly Type ConceptInfoType;

        public GenericParser(Type conceptInfoType, string keyword)
        {
            ConceptInfoType = conceptInfoType;
            Keyword = keyword;
            Members = ConceptMembers.Get(conceptInfoType).ToArray();
        }

        public virtual ValueOrError<IConceptInfo> Parse(ITokenReader tokenReader, Stack<IConceptInfo> context)
        {
            if (tokenReader.TryRead(Keyword))
            {
                var lastConcept = context.Count > 0 ? context.Peek() : null;
                return ParseMembers(tokenReader, lastConcept, false);
            }
            else
                return ValueOrError<IConceptInfo>.CreateError("");
        }

        private ValueOrError<IConceptInfo> ParseMembers(ITokenReader tokenReader, IConceptInfo useLastConcept, bool readingAReference)
        {
            IConceptInfo conceptInfo = (IConceptInfo)Activator.CreateInstance(ConceptInfoType);
            bool firstMember = true;
            bool lastPropertyWasInlineParent = false;

            var listOfMembers = readingAReference ? Members.Where(m => m.IsKey) : Members.Where(m => m.IsParsable);

            var parentProperty = listOfMembers.LastOrDefault(member => member.IsParentNested)
                ?? (listOfMembers.First().IsConceptInfo ? listOfMembers.First() : null);

            if (useLastConcept != null && parentProperty == null)
                return ValueOrError<IConceptInfo>.CreateError($"This concept cannot be nested within {useLastConcept.GetType().Name}. Trying to read {ConceptInfoType.Name}.");

            foreach (ConceptMember member in listOfMembers)
            {
                var valueOrError = ReadMemberValue(member, tokenReader, member == parentProperty ? useLastConcept : null, firstMember, ref lastPropertyWasInlineParent, readingAReference);

                if (valueOrError.IsError)
                    return ValueOrError<IConceptInfo>.CreateError(string.Format(CultureInfo.InvariantCulture,
                        "Cannot read the value of {0} in {1}. {2}",
                        member.Name, ConceptInfoType.Name, valueOrError.Error));

                member.SetMemberValue(conceptInfo, valueOrError.Value);
                firstMember = false;
            }

            return ValueOrError<IConceptInfo>.CreateValue(conceptInfo);
        }

        private ValueOrError<object> ReadMemberValue(ConceptMember member, ITokenReader tokenReader, IConceptInfo useLastConcept,
            bool firstMember, ref bool lastPropertyWasInlineParent, bool readingAReference)
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
                {
                    if (useLastConcept == null)
                        return tokenReader.ReadText().ChangeType<object>();
                    else
                        return ValueOrError<object>.CreateError($"This concept cannot be nested within {useLastConcept.GetType().Name}. Trying to read {ConceptInfoType.Name}.");
                }

                if (member.ValueType == typeof(IConceptInfo))
                {
                    if (useLastConcept != null)
                        return (object)useLastConcept;
                    else
                        return ValueOrError<object>.CreateError($"Member of type IConceptInfo can only be nested within" +
                            $" the referenced parent concept. It must be a first member or marked with {nameof(ConceptParentAttribute)}.");
                }

                if (useLastConcept != null && member.ValueType.IsInstanceOfType(useLastConcept))
                    return (object)useLastConcept;
                else if (firstMember)
                    lastPropertyWasInlineParent = true;

                if (member.IsConceptInfo && firstMember && !readingAReference && Members.Count(m => m.IsParsable) == 1)
                {
                    // This validation is not necessary for consistent parsing. It is enforced simply to avoid ambiguity for future concept overloads
                    // when parsing similar concepts such as "Logging { AllProperties; }", "History { AllProperties; }" and "Persisted { AllProperties; }".
                    var parentMembers = ConceptMembers.Get(member.ValueType).Where(m => m.IsParsable).ToArray();
                    if (parentMembers.Count() == 1 && parentMembers.Single().IsConceptInfo)
                        return ValueOrError.CreateError($"{ConceptInfoHelper.GetKeywordOrTypeName(ConceptInfoType)} must be nested" +
                            $" within the referenced parent concept {ConceptInfoHelper.GetKeywordOrTypeName(member.ValueType)}." +
                            $" A single-reference concept that references another single-reference concept must always be used with nested syntax to avoid ambiguity.");
                }

                if (member.IsConceptInfo)
                {
                    if (firstMember && member.ValueType == ConceptInfoType)
                        return ValueOrError.CreateError(string.Format(
                            "Recursive concept {0} cannot be used as a root because its parent property ({1}) must reference another concept. Use a non-recursive concept for the root and a derivation of the root concept with additional parent property as a recursive concept.",
                            ConceptInfoHelper.GetKeywordOrTypeName(ConceptInfoType), member.Name));

                    GenericParser subParser = new GenericParser(member.ValueType, "");
                    return subParser.ParseMembers(tokenReader, useLastConcept, true).ChangeType<object>();
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