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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(KeyPropertyComputedFromInfo))]
    public class KeyPropertyComputedFromCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeyPropertyComputedFromInfo)conceptInfo;
            if (IsSupported(info.PropertyComputedFrom.Target))
                codeBuilder.InsertCode(Snippet(info), AlternativeKeyComparerCodeGenerator.CompareKeyPropertyTag, info.Dependency_AlternativeKeyComparer);
        }

        private static readonly Dictionary<Type, string> _supportedSnippets = new Dictionary<Type, string>
        {
            { typeof(BoolPropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(DatePropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(DateTimePropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(DecimalPropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(GuidPropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(IntegerPropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },
            { typeof(MoneyPropertyInfo), "x.{0} == null ? (y.{0} == null ? 0 : -1) : x.{0}.Value.CompareTo(y.{0})" },

            { typeof(ReferencePropertyInfo), "x.{0}ID == null ? (y.{0}ID == null ? 0 : -1) : x.{0}ID.Value.CompareTo(y.{0}ID)" },

            { typeof(LongStringPropertyInfo), "string.Compare(x.{0}, y.{0}, StringComparison.InvariantCultureIgnoreCase)" }, // Handles null values well.
            { typeof(ShortStringPropertyInfo), "string.Compare(x.{0}, y.{0}, StringComparison.InvariantCultureIgnoreCase)" }, // Handles null values well.

            //{ typeof(BinaryPropertyInfo), "x.{0}.CompareTo(y.{0})" },
            //{ typeof(LinkedItemsInfo), "x.{0}.CompareTo(y.{0})" },
        };

        public bool IsSupported(PropertyInfo targetProperty)
        {
            return _supportedSnippets.Keys.Any(type => type.IsAssignableFrom(targetProperty.GetType()));
        }

        private static string Snippet(KeyPropertyComputedFromInfo info)
        {
            Type targetPropertyType = info.PropertyComputedFrom.Target.GetType();
            var diffSnippet = _supportedSnippets.First(snippet => snippet.Key.IsAssignableFrom(targetPropertyType)).Value;

            return string.Format(
                "diff = " + diffSnippet + @";
                if (diff != 0) return diff;
                ",
                info.PropertyComputedFrom.Target.Name);
        }
    }
}
