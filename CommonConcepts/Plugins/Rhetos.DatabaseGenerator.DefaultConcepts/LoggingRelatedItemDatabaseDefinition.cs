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
    [ExportMetadata(MefProvider.Implements, typeof(LoggingRelatedItemInfo))]
    public class LoggingRelatedItemDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            if (Sql.TryGet("LoggingRelatedItemDatabaseDefinition_TempColumnDefinition") == null)
            {
                createdDependencies = null;
                return;
            }

            var info = (LoggingRelatedItemInfo)conceptInfo;

            InsertCode(codeBuilder, info, "LoggingRelatedItemDatabaseDefinition_TempColumnDefinition", EntityLoggingDefinition.TempColumnDefinitionTag);
            InsertCode(codeBuilder, info, "LoggingRelatedItemDatabaseDefinition_TempColumnList", EntityLoggingDefinition.TempColumnListTag);
            InsertCode(codeBuilder, info, "LoggingRelatedItemDatabaseDefinition_TempColumnSelect", EntityLoggingDefinition.TempColumnSelectTag);
            InsertCode(codeBuilder, info, "LoggingRelatedItemDatabaseDefinition_AfterInsertLog", EntityLoggingDefinition.AfterInsertLogTag);

            IConceptInfo logRelatedItemTableMustBeFullyCreated = new PrerequisiteAllProperties { DependsOn = new EntityInfo { Module = new ModuleInfo { Name = "Common" }, Name = "LogRelatedItem" } };
            createdDependencies = new[] { Tuple.Create(logRelatedItemTableMustBeFullyCreated, conceptInfo) };
        }

        private static void InsertCode(ICodeBuilder codeBuilder, LoggingRelatedItemInfo info, string sqlResource, SqlTag<EntityLoggingInfo> tag)
        {
            string codeSnippet = Sql.Format(sqlResource,
                GetTempColumnNameOld(info),
                GetTempColumnNameNew(info),
                info.Table,
                SqlUtility.Identifier(info.Column),
                SqlUtility.QuoteText(info.Relation));

            codeBuilder.InsertCode(codeSnippet, tag, info.Logging);
        }

        private static string GetTempColumnNameOld(LoggingRelatedItemInfo info)
        {
            return SqlUtility.Identifier("Old_" + CsUtility.TextToIdentifier(info.Relation) + "_" + info.Column);
        }

        private static string GetTempColumnNameNew(LoggingRelatedItemInfo info)
        {
            return SqlUtility.Identifier("New_" + CsUtility.TextToIdentifier(info.Relation) + "_" + info.Column);
        }
    }
}
