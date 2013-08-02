﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Text;

namespace Rhetos.Dsl
{
    public static class ConceptInfoHelper
    {
        /// <summary>
        /// Returns a string that <b>uniquely describes the concept instance</b>.
        /// The string contains concept's base class type and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// If the concept inherits another concept, the base class type will be used instead of
        /// actual concept's type to achieve normalized form. That way, the resulting string
        /// can be used in scenarios such as resolving references to other concepts where
        /// a reference can be of the base class type, but referencing inherited type.
        /// </remarks>
        public static string GetKey(this IConceptInfo ci)
        {
            if (ci == null)
                throw new ArgumentNullException();

            StringBuilder desc = new StringBuilder(100);
            desc.Append(BaseConceptInfoType(ci).Name);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        public static string GetShortDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            desc.Append(ci.GetType().Name);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string that describes the concept instance in a user-friendly manner.
        /// The string contains concept's keyword and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// This description in not unique because different concepts might have same keyword.
        /// </remarks>
        public static string GetUserDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            desc.Append(GetKeywordOrTypeName(ci));
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string with a dot-separated list of concept's key properties.
        /// </summary>
        public static string GetKeyProperties(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(100);
            AppendMembers(desc, ci, SerializationOptions.KeyMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Returns a string that fully describes the concept instance.
        /// The string contains concept's type name and all concept's properties.
        /// </summary>
        public static string GetFullDescription(this IConceptInfo ci)
        {
            StringBuilder desc = new StringBuilder(200);
            desc.Append(ci.GetType().FullName);
            desc.Append(" ");
            AppendMembers(desc, ci, SerializationOptions.AllMembers);
            return desc.ToString();
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(this IConceptInfo ci)
        {
            return GetKeyword(ci.GetType());
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(Type conceptInfoType)
        {
            return conceptInfoType
                .GetCustomAttributes(typeof(ConceptKeywordAttribute), false)
                .Select(keywordAttribute => ((ConceptKeywordAttribute)keywordAttribute).Keyword)
                .SingleOrDefault();
        }

        public static string GetKeywordOrTypeName(this IConceptInfo ci)
        {
            return ci.GetKeyword() ?? ci.GetType().Name;
        }

        public static string GetKeywordOrTypeName(Type conceptInfoType)
        {
            return GetKeyword(conceptInfoType) ?? conceptInfoType.Name;
        }

        /// <summary>
        /// Returns a list of concepts that this concept directly depends on.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetDirectDependencies(this IConceptInfo conceptInfo)
        {
            return (from member in ConceptMembers.Get(conceptInfo)
                    where member.IsConceptInfo
                    select (IConceptInfo) member.GetValue(conceptInfo)).Distinct().ToList();
        }

        /// <summary>
        /// Returns a list of concepts that this concept depends on directly or indirectly.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetAllDependencies(this IConceptInfo conceptInfo)
        {
            var dependencies = new List<IConceptInfo>();
            AddAllDependencies(conceptInfo, dependencies);
            return dependencies;
        }

        /// <summary>
        /// Use only for generating an error details. Returns the concept's description ignoring possible null reference errors.
        /// </summary>
        public static string GetErrorDescription(this IConceptInfo ci)
        {
            if (ci == null)
                return "<null>";
            var report = new StringBuilder();
            report.Append(ci.GetType().FullName);
            foreach (var member in ConceptMembers.Get(ci))
            {
                report.Append(" " + member.Name + "=");
                var memberValue = member.GetValue(ci);
                try
                {
                    if (memberValue == null)
                        report.Append("<null>");
                    else if (member.IsConceptInfo)
                        report.Append(((IConceptInfo)memberValue).GetKeyProperties());
                    else
                        report.Append(memberValue.ToString());
                }
                catch (Exception ex)
                {
                    report.Append("<" + ex.GetType().Name + ">");
                }
            }
            return report.ToString();
        }


        private static void AddAllDependencies(IConceptInfo conceptInfo, ICollection<IConceptInfo> dependencies)
        {
            foreach (var member in ConceptMembers.Get(conceptInfo))
                if (member.IsConceptInfo)
                {
                    var dependency = (IConceptInfo)member.GetValue(conceptInfo);
                    if (!dependencies.Contains(dependency))
                    {
                        dependencies.Add(dependency);
                        AddAllDependencies(dependency, dependencies);
                    }
                }
        }

        private enum SerializationOptions
        {
            KeyMembers,
            AllMembers
        };

        private static void AppendMembers(StringBuilder text, IConceptInfo ci, SerializationOptions serializationOptions)
        {
            IEnumerable<ConceptMember> members = ConceptMembers.Get(ci);
            if (serializationOptions == SerializationOptions.KeyMembers)
                members = members.Where(member => member.IsKey);

            bool firstMember = true;
            foreach (ConceptMember member in members)
            {
                string separator = member.IsKey ? "." : " ";
                if (!firstMember)
                    text.Append(separator);
                firstMember = false;

                AppendMember(text, ci, member);
            }
        }

        private static void AppendMember(StringBuilder text, IConceptInfo ci, ConceptMember member)
        {
            object memberValue = member.GetValue(ci);
            if (memberValue == null)
                text.Append("<null>");
            else if (member.IsConceptInfo)
            {
                IConceptInfo value = (IConceptInfo) member.GetValue(ci);
                if (member.ValueType == typeof(IConceptInfo))
                    text.Append(BaseConceptInfoType(value).Name).Append(":");
                AppendMembers(text, value, SerializationOptions.KeyMembers);
            }
            else if (member.ValueType == typeof(string))
                text.Append(SafeDelimit(member.GetValue(ci).ToString()));
            else
                throw new FrameworkException(string.Format(
                    "IConceptInfo member {0} of type {1} in {2} is not supported.",
                    member.Name, member.ValueType.Name, ci.GetType().Name));
        }

        private static string SafeDelimit(string text)
        {
            bool clean = text.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c=='_');
            if (clean && text.Length > 0)
                return text;
            string quote = (text.Contains('\'') && !text.Contains('\"')) ? "\"" : "\'";
            return quote + text.Replace(quote, quote + quote) + quote;
        }

        private static Type BaseConceptInfoType(IConceptInfo ci)
        {
            Type t = ci.GetType();
            while (typeof(IConceptInfo).IsAssignableFrom(t.BaseType) && t.BaseType.IsClass)
                t = t.BaseType;
            return t;
        }
    }
}