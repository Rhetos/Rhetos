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
    [ExportMetadata(MefProvider.Implements, typeof(PropertyLoggingInfo))]
    public class PropertyLoggingDefinition : IConceptDatabaseDefinitionExtension
    {
        private static string GetColumnName(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return SqlUtility.Identifier(property.Name + "ID");

            return SqlUtility.Identifier(property.Name);
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (PropertyLoggingInfo)conceptInfo;
            var column = GetColumnName(info.Property);

            string propertyTypeKeyword = info.Property.GetKeywordOrTypeName();
            string propertyToStringSnippet = Sql.TryGet("PropertyLoggingDefinition_TextValue_" + propertyTypeKeyword);
            if (string.IsNullOrEmpty(propertyToStringSnippet))
                propertyToStringSnippet = Sql.Get("PropertyLoggingDefinition_TextValue");
            var propertyToString = string.Format(propertyToStringSnippet, column);

            codeBuilder.InsertCode(
                Sql.Format("PropertyLoggingDefinition_GenericPropertyLogging",
                    column,
                    propertyToString),
                EntityLoggingDefinition.LogPropertyTag, info.EntityLogging);

            createdDependencies = new[] { Tuple.Create<IConceptInfo, IConceptInfo>(info.Property, info.EntityLogging) }; // Entity's property (table column) must be created before entity's logging trigger is created.
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }
    }
}