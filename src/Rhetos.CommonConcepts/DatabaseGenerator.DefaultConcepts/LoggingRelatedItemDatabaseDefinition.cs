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
using Rhetos.Utilities;
using System;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class LoggingRelatedItemDatabaseDefinition : IConceptDatabaseGenerator<LoggingRelatedItemInfo>
    {
        public void GenerateCode(LoggingRelatedItemInfo conceptInfo, ISqlCodeBuilder sql)
        {
            if (sql.Resources.TryGet("LoggingRelatedItemDatabaseDefinition_TempColumnDefinition") == null)
                return;

            string tempColumnNameOld = sql.Utility.Identifier("Old_" + CsUtility.TextToIdentifier(conceptInfo.Relation) + "_" + conceptInfo.Column);
            string tempColumnNameNew = sql.Utility.Identifier("New_" + CsUtility.TextToIdentifier(conceptInfo.Relation) + "_" + conceptInfo.Column);

            var snippets = new[]
            {
                (sqlResource: "LoggingRelatedItemDatabaseDefinition_TempColumnDefinition", tag: EntityLoggingDefinition.TempColumnDefinitionTag),
                (sqlResource: "LoggingRelatedItemDatabaseDefinition_TempColumnList", tag: EntityLoggingDefinition.TempColumnListTag),
                (sqlResource: "LoggingRelatedItemDatabaseDefinition_TempColumnSelect", tag: EntityLoggingDefinition.TempColumnSelectTag),
                (sqlResource: "LoggingRelatedItemDatabaseDefinition_AfterInsertLog", tag: EntityLoggingDefinition.AfterInsertLogTag),
            };

            foreach (var snippet in snippets)
            {
                string codeSnippet = sql.Resources.Format(snippet.sqlResource,
                    tempColumnNameOld,
                    tempColumnNameNew,
                    conceptInfo.Table,
                    sql.Utility.Identifier(conceptInfo.Column),
                    sql.Utility.QuoteText(conceptInfo.Relation));

                sql.CodeBuilder.InsertCode(codeSnippet, snippet.tag, conceptInfo.Logging);
            }

            IConceptInfo logRelatedItemTableMustBeFullyCreated = new PrerequisiteAllProperties { DependsOn = new EntityInfo { Module = new ModuleInfo { Name = "Common" }, Name = "LogRelatedItem" } };
            sql.AddDependencies([ Tuple.Create(logRelatedItemTableMustBeFullyCreated, (IConceptInfo)conceptInfo) ]);
        }
    }
}
