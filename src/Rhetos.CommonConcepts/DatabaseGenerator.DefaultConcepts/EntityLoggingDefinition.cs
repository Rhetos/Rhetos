﻿/*
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

        private readonly ISqlResources _sqlResources;
        private readonly ISqlUtility _sqlUtility;

        public EntityLoggingDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            _sqlResources = sqlResources;
            _sqlUtility = sqlUtility;
        }

        public string GetTriggerNameInsert(EntityLoggingInfo conceptInfo)
        {
            return _sqlUtility.Identifier(_sqlResources.Format(
                "EntityLoggingDefinition_TriggerNameInsert",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public string GetTriggerNameUpdate(EntityLoggingInfo conceptInfo)
        {
            return _sqlUtility.Identifier(_sqlResources.Format(
                "EntityLoggingDefinition_TriggerNameUpdate",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public string GetTriggerNameDelete(EntityLoggingInfo conceptInfo)
        {
            return _sqlUtility.Identifier(_sqlResources.Format(
                "EntityLoggingDefinition_TriggerNameDelete",
                conceptInfo.Entity.Module.Name,
                conceptInfo.Entity.Name));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityLoggingInfo)conceptInfo;

            return _sqlResources.Format("EntityLoggingDefinition_Create",
                _sqlUtility.Identifier(info.Entity.Module.Name),
                _sqlUtility.Identifier(info.Entity.Name),
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

            return _sqlResources.Format("EntityLoggingDefinition_Remove",
                _sqlUtility.Identifier(info.Entity.Module.Name),
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
