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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodeForEachInfo))]
    public class AutoCodeForEachCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (AutoCodeForEachInfo)conceptInfo;

            // The groupSelector value format must be compatible with T-SQL data formatting,
            // because it will be used is a generated SQL query to filter the records within the same group.
            string groupSelector = SnippetSqlFormat(info.Group);
            string groupingSnippet = ", " + groupSelector;
            codeBuilder.InsertCode(groupingSnippet, AutoCodePropertyCodeGenerator.GroupingTag, info);

            string groupColumnName = info.Group.Name + (info.Group is ReferencePropertyInfo ? "ID" : "");
            string groupQuoted = SnippetSqlQuoted(info.Group) ? "true" : "false";
            string groupingMetadataSnippet = $",\r\n                {CsUtility.QuotedString(groupColumnName)}, {groupQuoted}";
            codeBuilder.InsertCode(groupingMetadataSnippet, AutoCodePropertyCodeGenerator.GroupColumnMetadataTag, info);
        }

        public static string SnippetSqlFormat(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return $"Rhetos.Utilities.SqlUtility.GuidToString(item.{property.Name}ID)";
            if (property is GuidPropertyInfo)
                return $"Rhetos.Utilities.SqlUtility.GuidToString(item.{property.Name})";
            else if (property is ShortStringPropertyInfo || property is LongStringPropertyInfo)
                return $"item.{property.Name}";
            else if (property is DateTimePropertyInfo || property is DatePropertyInfo)
                return $"Rhetos.Utilities.SqlUtility.DateTimeToString(item.{property.Name})";
            else if (property is BoolPropertyInfo)
                return $"Rhetos.Utilities.SqlUtility.BoolToString(item.{property.Name})";
            else if (property is BinaryPropertyInfo)
                return $"Rhetos.Utilities.SqlUtility.ByteArrayToString(item.{property.Name})";
            else
                return $"item.{property.Name} != null ? item.{property.Name}.ToString() : null";
        }

        public static bool SnippetSqlQuoted(PropertyInfo property)
        {
            return !(property is BoolPropertyInfo
                || property is IntegerPropertyInfo
                || property is DecimalPropertyInfo
                || property is MoneyPropertyInfo
                || property is BinaryPropertyInfo);
        }
    }
}
