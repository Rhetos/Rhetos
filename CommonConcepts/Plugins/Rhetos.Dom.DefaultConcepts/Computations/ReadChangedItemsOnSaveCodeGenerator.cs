/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReadChangedItemsOnSaveInfo))]
    public class ReadChangedItemsOnSaveCodeGenerator : IConceptCodeGenerator
    {
        public static readonly DataStructureCodeGenerator.DataStructureTag BeforeSaveUseChangedItems =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*ReadChangedItemsOnSave BeforeSaveUseChangedItems {0}.{1}*/");

        public static readonly DataStructureCodeGenerator.DataStructureTag AfterSaveUseChangedItems =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*ReadChangedItemsOnSave AlterSaveUseChangedItems {0}.{1}*/");

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReadChangedItemsOnSaveInfo) conceptInfo;
            codeBuilder.InsertCode(BeforeSaveSnippet(info.DataStructure), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.DataStructure);
            codeBuilder.InsertCode(AfterSaveSnippet(info.DataStructure), WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.DataStructure);
        }

        private static string BeforeSaveSnippet(DataStructureInfo info)
        {
            return string.Format(
@"            {0}.{1}[] changedItemsOld = updated.Concat(deleted).ToArray();

{2}

            changedItemsOld = null;

",
                info.Module.Name, info.Name, BeforeSaveUseChangedItems.Evaluate(info));
        }

        private static string AfterSaveSnippet(DataStructureInfo info)
        {
            return string.Format(
@"            {0}.{1}[] changedItemsNew = inserted.Concat(updated).ToArray();

{2}

",
                info.Module.Name, info.Name, AfterSaveUseChangedItems.Evaluate(info));
        }
    }
}
