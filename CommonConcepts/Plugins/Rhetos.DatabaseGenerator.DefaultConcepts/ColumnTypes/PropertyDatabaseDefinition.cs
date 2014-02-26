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
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    /// <summary>
    /// This class is a helper for implementations of different property types.
    /// </summary>
    public static class PropertyDatabaseDefinition
    {
        /// <summary>Ordering of options may be important./summary>
        public static readonly SqlTag<PropertyInfo> Options1Tag = "Options1";
        /// <summary>Ordering of options may be important./summary>
        public static readonly SqlTag<PropertyInfo> Options2Tag = "Options2";

        public static string AddColumn(PropertyInfo property, string columnName, string type, string options = "")
        {
            return Sql.Format("PropertyDatabaseDefinition_AddColumn",
                SqlUtility.Identifier(property.DataStructure.Module.Name),
                SqlUtility.Identifier(property.DataStructure.Name),
                SqlUtility.CheckIdentifier(columnName),
                type,
                options,
                Options1Tag.Evaluate(property),
                Options2Tag.Evaluate(property));
        }

        public static string RemoveColumn(PropertyInfo property, string columnName)
        {
            return Sql.Format("PropertyDatabaseDefinition_RemoveColumn",
                SqlUtility.Identifier(property.DataStructure.Module.Name),
                SqlUtility.Identifier(property.DataStructure.Name),
                SqlUtility.CheckIdentifier(columnName));
        }
    }
}
