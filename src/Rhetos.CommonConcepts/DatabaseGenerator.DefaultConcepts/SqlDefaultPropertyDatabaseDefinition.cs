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
using System.ComponentModel.Composition;
using System.Globalization;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{

    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlDefaultPropertyInfo))]
    public class SqlDefaultPropertyDatabaseDefinition : IConceptDatabaseDefinition
    {
        public static string GetConstraintName(PropertyInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("SqlDefaultPropertyDatabaseDefinition_ConstraintName", info.DataStructure.Name, info.Name));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlDefaultPropertyInfo)conceptInfo;

            if (info.Property.DataStructure is EntityInfo)
                return Sql.Format("SqlDefaultPropertyDatabaseDefinition_Create",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    SqlUtility.Identifier(info.Property.Name),
                    GetConstraintName(info.Property),
                    info.Definition);
            return "";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlDefaultPropertyInfo)conceptInfo;
            if (info.Property.DataStructure is EntityInfo)
                return Sql.Format("SqlDefaultPropertyDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    SqlUtility.Identifier(info.Property.Name),
                    GetConstraintName(info.Property),
                    info.Definition);
            return "";
        }
    }
}