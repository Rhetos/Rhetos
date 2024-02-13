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
using System.ComponentModel.Composition;

namespace Rhetos.EfCore.ModelBuilding
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyModelCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ReferencePropertyInfo> ModelWithManyTag = "ModelWithMany";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReferencePropertyInfo)conceptInfo;

            if (info.DataStructure is IOrmDataStructure)
            {
                if (info.Referenced is IOrmDataStructure)
                {
                    codeBuilder.InsertCode(
                        $"\r\n                entity.HasOne(e => e.{info.Name})" +
                        $".WithMany({ModelWithManyTag.Evaluate(info)})" +
                        $".HasForeignKey(e => e.{info.Name}ID)" +
                        $"{PropertyModelCodeGenerator.ModelOptionsTag.Evaluate(info)};",
                        DataStructureModelCodeGenerator.ModelBuilderTag, info.DataStructure);
                }
                else
                {
                    codeBuilder.InsertCode(
                        $"\r\n                entity.Property(e => e.{info.Name}ID).HasColumnType(\"uniqueidentifier\"){PropertyModelCodeGenerator.ModelOptionsTag.Evaluate(info)};",
                        DataStructureModelCodeGenerator.ModelBuilderTag, info.DataStructure);
                }
            }
        }
    }
}
