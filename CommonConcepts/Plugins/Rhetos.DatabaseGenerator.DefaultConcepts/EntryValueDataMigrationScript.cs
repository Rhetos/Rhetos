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
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IDataMigrationScript))]
    [ExportMetadata(MefProvider.Implements, typeof(EntryValueInfo))]
    public class EntryValueDataMigrationScript : IDataMigrationScript<EntryValueInfo>
    {
        TypeExtensionProvider _typeExtensionprovider;

        public EntryValueDataMigrationScript(TypeExtensionProvider typeExtensionprovider)
        {
            _typeExtensionprovider = typeExtensionprovider;
        }

        public void GenerateCode(EntryValueInfo concept, IDataMigrationScriptBuilder codeBuilder)
        {
            var databaseColumnName = _typeExtensionprovider.Get<IDatabaseColumnName<PropertyInfo>>(concept.Property.GetType()).GetColumnName(concept.Property);
            var databaseColumnType = _typeExtensionprovider.Get<IDatabaseColumnType<PropertyInfo>>(concept.Property.GetType()).ColumnType;

            codeBuilder.InsertCode($@", {Environment.NewLine}{databaseColumnName} = CONVERT({databaseColumnType}, '{concept.Value}')",
                EntryDataMigrationScript.UpdatePropertyTag, concept.Entry);
        }
    }
}
