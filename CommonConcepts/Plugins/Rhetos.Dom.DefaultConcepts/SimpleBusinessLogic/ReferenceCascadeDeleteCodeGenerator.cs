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

using Rhetos.Dsl.DefaultConcepts;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferenceCascadeDeleteInfo))]
    public class ReferenceCascadeDeleteCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReferenceCascadeDeleteInfo)conceptInfo;

            InsertCodeSnippet(codeBuilder, info.Reference, info.Reference.Referenced);
        }

        public static void InsertCodeSnippet(ICodeBuilder codeBuilder, ReferencePropertyInfo reference, DataStructureInfo parent)
        {
            if (reference.DataStructure is IWritableOrmDataStructure && parent is IWritableOrmDataStructure)
            {
                string detailName = reference.DataStructure.Module.Name + "." + reference.DataStructure.Name;

                string snippetDeleteChildItems =
            $@"if (deletedIds.Count() > 0)
            {{
                List<{detailName}> childItems = deletedIds
                    .SelectMany(parent => _executionContext.Repository.{detailName}.Query()
                        .Where(child => child.{reference.Name}ID == parent.ID)
                        .Select(child => child.ID)
                        .ToList())
                    .Select(childId => new {detailName} {{ ID = childId }})
                    .ToList();

                if (childItems.Count() > 0)
                    _domRepository.{detailName}.Delete(childItems);
            }}

            ";

                codeBuilder.InsertCode(snippetDeleteChildItems, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, parent);
            }
        }
    }
}
