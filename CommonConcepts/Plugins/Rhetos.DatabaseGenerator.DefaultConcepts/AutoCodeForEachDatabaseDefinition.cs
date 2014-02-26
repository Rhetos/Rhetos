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
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodeForEachInfo))]
    public class AutoCodeForEachDatabaseDefinition : IConceptDatabaseDefinitionExtension
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
            var info = (AutoCodeForEachInfo) conceptInfo;
            createdDependencies = null;

            if (AutoCodeDatabaseDefinition.IsSupported(info.Property))
            {
                codeBuilder.InsertCode(Sql.Format("AutoCodeForEachDatabaseDefinition_ExtendBeforeCursor", ShortStringPropertyInfo.MaxLength),
                    AutoCodeDatabaseDefinition.BeforeCursorTag, info);

                string resourceName = GetResourceNameForCursorSelectSnippet(info.Group);
                codeBuilder.InsertCode(Sql.Format(resourceName, GetColumnName(info.Group), ShortStringPropertyInfo.MaxLength),
                    AutoCodeDatabaseDefinition.CursorSelectTag, info);

                codeBuilder.InsertCode(Sql.Format("AutoCodeForEachDatabaseDefinition_ExtendCursorFetch", GetColumnName(info.Group), ShortStringPropertyInfo.MaxLength),
                    AutoCodeDatabaseDefinition.CursorFetchTag, info);

                codeBuilder.InsertCode(Sql.Format("AutoCodeForEachDatabaseDefinition_ExtendBeforeGenerate", GetColumnName(info.Group), ShortStringPropertyInfo.MaxLength),
                    AutoCodeDatabaseDefinition.BeforeGenerateTag, info);
            }
        }

        private static string GetResourceNameForCursorSelectSnippet(PropertyInfo groupProperty)
        {
            var resourceName = "AutoCodeForEachDatabaseDefinition_ExtendCursorSelect_" + groupProperty.GetType().Name;
            try
            {
                Sql.Get(resourceName);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(
                    "Group property type '" + groupProperty.GetType().Name +
                    "' is not supported in AutoCodeForEachDatabaseDefinition. " + ex.Message, ex);
            }
            return resourceName;
        }

        private static string GetColumnName(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return SqlUtility.Identifier(property.Name + "ID");
            return SqlUtility.Identifier(property.Name);
        }
    }
}
