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
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.EfCore.ModelBuilding
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureModelCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> ModelBuilderTag = "ModelBuilder";
        private readonly ConceptMetadata _conceptMetadata;

        public DataStructureModelCodeGenerator(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info is IOrmDataStructure)
            {
                string code =
            $@"modelBuilder.Entity<Common.Queryable.{info.Module.Name}_{info.Name}>(entity => {{
                entity.ToTable({CsUtility.QuotedString(_conceptMetadata.GetOrmDatabaseObject(info))}, schema: {CsUtility.QuotedString(_conceptMetadata.GetOrmSchema(info))});{ModelBuilderTag.Evaluate(info)}
            }});

            ";

                codeBuilder.InsertCode(code, DbContextCodeGenerator.EntityFrameworkOnModelCreatingTag);
            }
        }
    }
}
