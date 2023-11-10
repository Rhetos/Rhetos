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

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    /// <summary>
    /// This class is a helper for implementations of different property types.
    /// </summary>
    public static class PropertyDatabaseDefinition
    {
        /// <summary>Ordering of options may be important.</summary>
        public static readonly SqlTag<PropertyInfo> Options1Tag = "Options1";
        /// <summary>Ordering of options may be important.</summary>
        public static readonly SqlTag<PropertyInfo> Options2Tag = "Options2";
        public static readonly SqlTag<PropertyInfo> AfterCreateTag = "AfterCreate";
        public static readonly SqlTag<PropertyInfo> BeforeRemoveTag = "BeforeRemove";

        public static string AddColumn(ISqlUtility sqlUtility, ISqlResources sqlResources, ConceptMetadata conceptMetadata, PropertyInfo property, string options = "")
        {
            string columnName = conceptMetadata.GetColumnName(property);

            return sqlResources.Format("PropertyDatabaseDefinition_AddColumn",
                sqlUtility.Identifier(property.DataStructure.Module.Name),
                sqlUtility.Identifier(property.DataStructure.Name),
                DslUtility.ValidateIdentifier(columnName, property, "Invalid column name."),
                conceptMetadata.GetColumnType(property),
                options,
                Options1Tag.Evaluate(property),
                Options2Tag.Evaluate(property),
                AfterCreateTag.Evaluate(property)).Trim();
        }

        public static string RemoveColumn(ISqlUtility sqlUtility, ISqlResources sqlResources, ConceptMetadata conceptMetadata, PropertyInfo property)
        {
            string columnName = conceptMetadata.GetColumnName(property);

            return sqlResources.Format("PropertyDatabaseDefinition_RemoveColumn",
                sqlUtility.Identifier(property.DataStructure.Module.Name),
                sqlUtility.Identifier(property.DataStructure.Name),
                DslUtility.ValidateIdentifier(columnName, property, "Invalid column name."),
                BeforeRemoveTag.Evaluate(property)).Trim();
        }
    }
}
