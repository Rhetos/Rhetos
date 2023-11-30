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
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Utilities;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDataMigration))]
    public class EntryValueDataMigrationScript : IConceptDataMigration<EntryValueInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public EntryValueDataMigrationScript(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(EntryValueInfo concept, IDataMigrationScriptBuilder codeBuilder)
        {
            var databaseColumnName = _conceptMetadata.GetColumnName(concept.Property);
            var databaseColumnType = _conceptMetadata.GetColumnType(concept.Property);

            codeBuilder.InsertCode($@",{Environment.NewLine}    {databaseColumnName} = CONVERT({databaseColumnType}, {SqlUtility.QuoteText(concept.Value)})",
                EntryDataMigrationScript.UpdatePropertyTag, concept.Entry);
        }
    }
}
