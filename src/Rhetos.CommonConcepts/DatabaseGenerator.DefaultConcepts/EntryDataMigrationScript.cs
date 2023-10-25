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

using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDataMigration))]
    public class EntryDataMigrationScript : IConceptDataMigration<EntryInfo>
    {
        public static readonly SqlTag<EntryInfo> UpdatePropertyTag = new SqlTag<EntryInfo>("UpdateProperty");

        public void GenerateCode(EntryInfo concept, IDataMigrationScriptBuilder codeBuilder)
        {
            string insertSnippet = $@"
INSERT INTO @entries (ID) VALUES ('{concept.GetIdentifier()}');";
            codeBuilder.InsertCode(insertSnippet, HardcodedEntityDataMigrationScript.InsertValuesTag, concept.HardcodedEntity);

            codeBuilder.InsertCode($@"

UPDATE _{concept.HardcodedEntity.Module.Name}.{concept.HardcodedEntity.Name}
SET Name = '{concept.Name}'{UpdatePropertyTag.Evaluate(concept)}
WHERE ID = '{concept.GetIdentifier()}';",
            HardcodedEntityDataMigrationScript.UpdateTag, concept.HardcodedEntity);
        }
    }
}
