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
using System.Text;
using System.Reflection;
using System.Globalization;

namespace Rhetos.Dsl
{
    public class ConceptMember
    {
        public string Name { get; private set; }
        public Type ValueType { get; private set; }
        public bool IsConceptInfo { get; private set; }
        public bool IsKey { get; private set; }
        public int SortOrder1 { get; private set; }
        public int SortOrder2 { get; private set; }
        public bool IsDerived { get; private set; }
        public bool IsStringType { get; private set; }
        public bool IsParsable { get; private set; }

        private MemberInfo MemberInfo;

        public ConceptMember(MemberInfo memberInfo, ISet<string> nonParsableMembers)
        {
            this.MemberInfo = memberInfo;

            this.Name = memberInfo.Name;
            this.ValueType = GetMemberType(memberInfo);
            this.IsConceptInfo = typeof(IConceptInfo).IsAssignableFrom(ValueType);
            this.IsKey = memberInfo.GetCustomAttributes(typeof(ConceptKeyAttribute), false).Any();
            this.SortOrder1 = -InheritanceDepth(memberInfo); // This is needed when the derived type is not in the same assembly as the base type. In that case, MetadataToken in not enough for sorting all properties.
            this.SortOrder2 = memberInfo.MetadataToken;

            this.IsDerived = memberInfo.DeclaringType != memberInfo.ReflectedType;
            this.IsStringType = ValueType == typeof(string);

            this.IsParsable = nonParsableMembers == null || !nonParsableMembers.Contains(memberInfo.Name);
        }

        private static int InheritanceDepth(MemberInfo memberInfo)
        {
            var baseType = memberInfo.DeclaringType;
            var derivedType = memberInfo.ReflectedType;

            int depth = 0;
            while (baseType != derivedType)
            {
                depth++;
                if (derivedType.BaseType == null)
                    throw new FrameworkException("Unexpected IConceptInfo property inheritance: Property " + memberInfo.Name
                        + " is declared in type " + memberInfo.DeclaringType.FullName + " and used in type " + memberInfo.ReflectedType.FullName
                        + ", but declaring type is not a base type of the other." );
                derivedType = derivedType.BaseType;
            }
            return depth;
        }

        public void SetMemberValue(IConceptInfo conceptInfo, object value)
        {
            PropertyInfo pi;
            FieldInfo fi;

            if ((pi = MemberInfo as PropertyInfo) != null)
                try
                {
                    pi.SetValue(conceptInfo, value, null);
                }
                catch (ArgumentException ae)
                {
                    throw new FrameworkException(
                        string.Format(CultureInfo.InvariantCulture,
                            "Unable to convert property {0} in concept {1} from type {2} to type {3}",
                                pi.Name,
                                conceptInfo.GetType().FullName,
                                pi.PropertyType.FullName,
                                value != null ? value.GetType().FullName : "unknown"),
                        ae);
                }
            else if ((fi = MemberInfo as FieldInfo) != null)
                try
                {
                    fi.SetValue(conceptInfo, value);
                }
                catch (ArgumentException ae)
                {
                    throw new FrameworkException(
                        string.Format(CultureInfo.InvariantCulture,
                            "Unable to convert property {0} in concept {1} from type {2} to type {3}",
                                pi.Name,
                                conceptInfo.GetType().FullName,
                                pi.PropertyType.FullName,
                                value != null ? value.GetType().FullName : "unknown"),
                        ae);
                }
            else
                throw new FrameworkException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Unexpected member type {0} for member {1}.",
                            MemberInfo.MemberType,
                            MemberInfo.Name));
        }

        private static Type GetMemberType(MemberInfo member)
        {
            PropertyInfo pi;
            if ((pi = member as PropertyInfo) != null)
                return pi.PropertyType;

            FieldInfo fi;
            if ((fi = member as FieldInfo) != null)
                return fi.FieldType;

            throw new FrameworkException(
                string.Format(CultureInfo.InvariantCulture,
                    "Unexpected member type {0} for member {1}.",
                        member.MemberType,
                        member.Name));
        }

        public override string ToString()
        {
            return Name + ":" + ValueType.Name;
        }

        public object GetValue(IConceptInfo conceptInfo)
        {
            PropertyInfo pi;
            FieldInfo fi;

            if ((pi = MemberInfo as PropertyInfo) != null)
                return pi.GetValue(conceptInfo, null);
            else if ((fi = MemberInfo as FieldInfo) != null)
                return fi.GetValue(conceptInfo);
            else
                throw new FrameworkException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Unexpected member type {0} for member {1}.",
                            MemberInfo.MemberType,
                            MemberInfo.Name));
        }
    }
}
