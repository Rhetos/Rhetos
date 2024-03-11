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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlObjectInfo))]
    public class SqlObjectDatabaseDefinition : IConceptDatabaseDefinition
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlObjectInfo)conceptInfo;
            return info.CreateSql
                .Replace("{SPLIT SCRIPT}", SqlUtility.ScriptSplitterTag); // Using 'Replace' instead of 'Format' function, to minimize unexpected errors for application developers (Format function expects all curly brackets to be escaped, e.g.).
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlObjectInfo)conceptInfo;
            return info.RemoveSql
                .Replace("{SPLIT SCRIPT}", SqlUtility.ScriptSplitterTag);
        }
    }
}
