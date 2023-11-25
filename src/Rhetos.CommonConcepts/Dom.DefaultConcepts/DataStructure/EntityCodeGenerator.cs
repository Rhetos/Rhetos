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
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityInfo))]
    public class EntityCodeGenerator : IConceptCodeGenerator
    {
        private readonly ISqlUtility _sqlUtility;
        private readonly ISqlResources _sqlResources;
        private readonly ConceptMetadata _conceptMetadata;

        public EntityCodeGenerator(ISqlUtility sqlUtility, ISqlResources sqlResources, ConceptMetadata conceptMetadata)
        {
            _sqlUtility = sqlUtility;
            _sqlResources = sqlResources;
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            EntityInfo info = (EntityInfo)conceptInfo;

            string pk = EntityDatabaseDefinition.PrimaryKeyConstraintName(info, _sqlUtility, _sqlResources);
            string table = _conceptMetadata.GetOrmSchema(info) + "." + _conceptMetadata.GetOrmDatabaseObject(info);

            codeBuilder.InsertCode($"({CsUtility.QuotedString(table)}, {CsUtility.QuotedString(pk)}) => \"ID\",\r\n                    ",
                WritableOrmDataStructureCodeGenerator.ErrorMetadataTag, info);
        }
    }
}
