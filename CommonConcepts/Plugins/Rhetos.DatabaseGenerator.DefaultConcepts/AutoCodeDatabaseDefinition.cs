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
using Rhetos.Utilities;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    /// <summary>
    /// This procedure returns the next available code based on the given code format and the existing records in the database.
    /// Supported format allows any prefix with a generated numerical suffix.

    /// Possible format types with examples (CodeFormat => NewCode):

    /// A) If the given format ends with "+", the new code will have the given prefix and the plus sign will be replaced with the next available number.
    /// Examples:
    /// "ab+" => "ab1"
    /// "ab+" => "ab2"
    /// "ab+" => "ab3"
    /// "c+" => "c1"
    /// "+" => "1"
    /// "+" => "2"
    /// Note: new code will maintain the length of the existing codes. For example, if the existing records contain code "ab005":
    /// "ab+" => "ab006"

    /// B) If the format doesn't end with "+", it is assumes the new code is explicitly defined.
    /// Examples:
    /// "123" => "123"
    /// "abc" => "abc"
    /// "" => ""

    /// C) If an unsupported format is given, the procedure will raise an error:
    /// Examples:
    /// "+++"
    /// "+123"

    /// Filter parameter:
    /// Filter is used in a case when the code is not unique in the table/view, but is unique within a certain group.
    /// For example, if the table contains column Family, and the codes are generated starting from 1 for each family,
    /// then when inserting a record in Family "X" the procedure should be called with the filter "Family = 'X'".
    /// </summary>
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodePropertyInfo))]
    [ConceptImplementationVersion(2, 0)]
    public class AutoCodeDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<AutoCodePropertyInfo> BeforeCursorTag = "BeforeCursor";
        public static readonly SqlTag<AutoCodePropertyInfo> CursorSelectTag = "CursorSelect";
        public static readonly SqlTag<AutoCodePropertyInfo> CursorFetchTag = "CursorFetch";
        public static readonly SqlTag<AutoCodePropertyInfo> BeforeGenerateTag = "BeforeGenerate";

        public static string TriggerName(PropertyInfo propertyInfo)
        {
            return SqlUtility.Identifier(Sql.Format("AutoCodeDatabaseDefinition_TriggerName",
                TableName(propertyInfo),
                propertyInfo.Name));
        }

        public static string TableName(PropertyInfo propertyInfo)
        {
            if (propertyInfo.DataStructure is EntityInfo)
                return SqlUtility.Identifier(propertyInfo.DataStructure.Name);
            throw new FrameworkException("AutoCode is not supported on the given data structure type: " + propertyInfo.DataStructure.GetUserDescription());
        }

        public static string TriggerSnippet(AutoCodePropertyInfo info)
        {
            return Sql.Format("AutoCodeDatabaseDefinition_TriggerSnippet",
                SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                TableName(info.Property),
                SqlUtility.Identifier(info.Property.Name),
                TriggerName(info.Property),
                ShortStringPropertyInfo.MaxLength,
                BeforeCursorTag.Evaluate(info),
                CursorSelectTag.Evaluate(info),
                CursorFetchTag.Evaluate(info),
                BeforeGenerateTag.Evaluate(info));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (AutoCodePropertyInfo) conceptInfo;

            if (IsSupported(info.Property))
                return TriggerSnippet(info);
            return null;
        }

        public static bool IsSupported(ShortStringPropertyInfo property)
        {
            return property.DataStructure is EntityInfo;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (AutoCodePropertyInfo) conceptInfo;

            if (IsSupported(info.Property))
                return DropTriggerSnippet(info);
            return null;
        }

        private static string DropTriggerSnippet(AutoCodePropertyInfo info)
        {
             return Sql.Format("AutoCodeDatabaseDefinition_Remove",
                SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                TriggerName(info.Property));
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (AutoCodePropertyInfo)conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();

            var usingSqlProcedure = new SqlProcedureInfo {
                Module = new ModuleInfo { Name = "Common" },
                Name = "GenerateNextAutoCode" };

            dependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(usingSqlProcedure, conceptInfo));

            createdDependencies = dependencies;
        }
    }
}
