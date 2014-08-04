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
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodeTriggerInfo))]
    [ConceptImplementationVersion(2, 0)]
    public class AutoCodeTriggerDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<AutoCodeTriggerInfo> ColumnsForAutoCodeSelectTag = "ColumnsForAutoCodeSelect";

        public static string TriggerName(DataStructureInfo dataStructureInfo)
        {
            return SqlUtility.Identifier(Sql.Format("AutoCodeDatabaseDefinition_TriggerName",
                dataStructureInfo.Module.Name,
                dataStructureInfo.Name));
        }

        public static string TriggerSnippet(AutoCodeTriggerInfo info)
        {
            return Sql.Format("AutoCodeDatabaseDefinition_TriggerSnippet",
                SqlUtility.Identifier(info.Entity.Module.Name),
                info.Entity.Name,
                TriggerName(info.Entity),
                ShortStringPropertyInfo.MaxLength,
                ColumnsForAutoCodeSelectTag.Evaluate(info));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (AutoCodeTriggerInfo)conceptInfo;

            if (IsSupported(info.Entity))
                return TriggerSnippet(info);
            return null;
        }

        public static bool IsSupported(DataStructureInfo dataStructure)
        {
            return dataStructure is EntityInfo;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (AutoCodeTriggerInfo)conceptInfo;

            if (IsSupported(info.Entity))
                return DropTriggerSnippet(info);
            return null;
        }

        private static string DropTriggerSnippet(AutoCodeTriggerInfo info)
        {
             return Sql.Format("AutoCodeDatabaseDefinition_Remove",
                SqlUtility.Identifier(info.Entity.Module.Name),
                TriggerName(info.Entity));
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (AutoCodeTriggerInfo)conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();

            var usingSqlProcedure = new SqlProcedureInfo {
                Module = new ModuleInfo { Name = "Common" },
                Name = "GenerateNextAutoCode" };

            dependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(usingSqlProcedure, conceptInfo));

            createdDependencies = dependencies;
        }
    }
}
