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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceCascadeDeleteInfo))]
    public class UniqueReferenceCascadeDeleteCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueReferenceCascadeDeleteInfo)conceptInfo;

            InsertCodeSnippet(codeBuilder, info.UniqueReference.Extension, info.UniqueReference.Base);
        }

        public static void InsertCodeSnippet(ICodeBuilder codeBuilder, DataStructureInfo extension, DataStructureInfo baseEntity)
        {
            if (baseEntity is IWritableOrmDataStructure && extension is IWritableOrmDataStructure)
            {
                string extensionName = extension.Module.Name + "." + extension.Name;

                string snippetDeleteChildItems =
            $@"if (deletedIds.Count() > 0)
            {{
                List<{extensionName}> childItems = _executionContext.Repository.{extensionName}
                    .Query(deletedIds.Select(parent => parent.ID))
                    .Select(child => child.ID).ToList()
                    .Select(childId => new {extensionName} {{ ID = childId }}).ToList();

                if (childItems.Count() > 0)
                    _domRepository.{extensionName}.Delete(childItems);
            }}

            ";

                codeBuilder.InsertCode(snippetDeleteChildItems, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, baseEntity);
            }
        }
    }
}
