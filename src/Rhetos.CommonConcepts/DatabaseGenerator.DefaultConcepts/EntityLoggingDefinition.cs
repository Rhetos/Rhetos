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
    [ExportMetadata(MefProvider.Implements, typeof(EntityLoggingInfo))]
    public class EntityLoggingDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<EntityLoggingInfo> LogPropertyTag = "LogProperty";
        public static readonly SqlTag<EntityLoggingInfo> TempColumnDefinitionTag = "TempColumnDefinition";
        public static readonly SqlTag<EntityLoggingInfo> TempColumnListTag = "TempColumnList";
        public static readonly SqlTag<EntityLoggingInfo> TempColumnSelectTag = "TempColumnSelect";
        public static readonly SqlTag<EntityLoggingInfo> TempFromTag = "TempFrom";
        public static readonly SqlTag<EntityLoggingInfo> AfterInsertLogTag = "AfterInsertLog";

        public static string GetTriggerNameInsert(EntityLoggingInfo conceptInfo)
        {
            return SqlUtility.Identifier(Sql.Format(
                "EntityLoggingDefinition_TriggerNameInsert",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public static string GetTriggerNameUpdate(EntityLoggingInfo conceptInfo)
        {
            return SqlUtility.Identifier(Sql.Format(
                "EntityLoggingDefinition_TriggerNameUpdate",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public static string GetTriggerNameDelete(EntityLoggingInfo conceptInfo)
        {
            return SqlUtility.Identifier(Sql.Format(
                "EntityLoggingDefinition_TriggerNameDelete",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityLoggingInfo)conceptInfo;

            return Sql.Format("EntityLoggingDefinition_Create",
                SqlUtility.Identifier(info.Entity.Module.Name),
                SqlUtility.Identifier(info.Entity.Name),
                GetTriggerNameInsert(info),
                GetTriggerNameUpdate(info),
                GetTriggerNameDelete(info),
                SqlUtility.ScriptSplitterTag,
                LogPropertyTag.Evaluate(info),
                TempColumnDefinitionTag.Evaluate(info),
                TempColumnListTag.Evaluate(info),
                TempColumnSelectTag.Evaluate(info),
                TempFromTag.Evaluate(info),
                AfterInsertLogTag.Evaluate(info));
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityLoggingInfo)conceptInfo;

            return Sql.Format("EntityLoggingDefinition_Remove",
                SqlUtility.Identifier(info.Entity.Module.Name),
                GetTriggerNameInsert(info),
                GetTriggerNameUpdate(info),
                GetTriggerNameDelete(info));
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            IConceptInfo logTableMustBeFullyCreated = new PrerequisiteAllProperties { DependsOn = new EntityInfo { Module = new ModuleInfo { Name = "Common" }, Name = "Log" } };
            createdDependencies = new[] { Tuple.Create(logTableMustBeFullyCreated, conceptInfo) }; // logTableMustBeFullyCreated before this logging trigger is created
        }
    }
}
