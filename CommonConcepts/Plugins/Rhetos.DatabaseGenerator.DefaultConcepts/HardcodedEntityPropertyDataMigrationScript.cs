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
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDataMigration))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    public class HardcodedEntityPropertyDataMigrationScript : IConceptDataMigration<PropertyInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public HardcodedEntityPropertyDataMigrationScript(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(PropertyInfo concept, IDataMigrationScriptBuilder codeBuilder)
        {
            if (concept.DataStructure is HardcodedEntityInfo)
            {
                var databaseColumnName = _conceptMetadata.GetColumnName(concept);
                var databaseColumnType = _conceptMetadata.GetColumnType(concept);

                codeBuilder.InsertCode(Environment.NewLine + $@"EXEC Rhetos.DataMigrationUse '{concept.DataStructure.Module.Name}', '{concept.DataStructure.Name}', '{databaseColumnName}', '{databaseColumnType}';",
                    HardcodedEntityDataMigrationScript.DataMigrationUseTag, concept.DataStructure as HardcodedEntityInfo);

                codeBuilder.InsertCode($@" + ', {databaseColumnName}'",
                    HardcodedEntityDataMigrationScript.DataMigrationApplyMultipleTag, concept.DataStructure as HardcodedEntityInfo);
            }
        }
    }
}
