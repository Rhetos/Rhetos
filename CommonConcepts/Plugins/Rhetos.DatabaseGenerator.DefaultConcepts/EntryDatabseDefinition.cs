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
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(NamedEntityEntryInfo))]
    public class EntryDatabseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<NamedEntityEntryInfo> PropertyValueTag = new SqlTag<NamedEntityEntryInfo>("PropertyValue");

        IDslModel _dslModel;

        public EntryDatabseDefinition(IDslModel dslModel)
        {
            _dslModel = dslModel;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (NamedEntityEntryInfo)conceptInfo;
            return $@"
UPDATE {info.NamedEntity.Module.Name}.{info.NamedEntity.Name}
SET
    Name = '{info.Name}'{PropertyValueTag.Evaluate(info)}
WHERE ID = '{info.GetIdentifier()}'";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (NamedEntityEntryInfo)conceptInfo;
            createdDependencies = _dslModel.FindByReference<PropertyInfo>(x => x.DataStructure, info.NamedEntity).Select(x => new Tuple<IConceptInfo, IConceptInfo>(x, info));
            if (codeBuilder.TagExists(NamedEntityDatabseDefinition.FirstValueTag.Evaluate(info.NamedEntity)))
            {
                codeBuilder.ReplaceCode(GenerateCodeSnippet(info), NamedEntityDatabseDefinition.FirstValueTag.Evaluate(info.NamedEntity));
            }
            else
            {
                codeBuilder.InsertCode($@" UNION ALL
	                {GenerateCodeSnippet(info)}", NamedEntityDatabseDefinition.ValuesTag, info.NamedEntity);
            }
            
        }

        private static string GenerateCodeSnippet(NamedEntityEntryInfo entryInfo)
        {
            return $@"SELECT ID = '{entryInfo.GetIdentifier()}'";
        }
    }
}
