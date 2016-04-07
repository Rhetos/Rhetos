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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LockItemsExceptPropertiesInfo))]
    public class LockItemsExceptPropertiesCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<LockItemsExceptPropertiesInfo> ComparePropertyTag = "CompareProperty";
        public static readonly CsTag<LockItemsExceptPropertiesInfo> ClientMessageTag = "ClientMessage";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LockItemsExceptPropertiesInfo)conceptInfo;
            codeBuilder.InsertCode(CheckLockedItemsSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Source);
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string CheckLockedItemsSnippet(LockItemsExceptPropertiesInfo info)
        {
            return string.Format(
            @"if (updatedNew.Count() > 0 || deletedIds.Count() > 0)
            {{
                {0}[] changedItems = updated.Zip(updatedNew, (i, j) => (false{4})
                    ? i : null).Where(x => x != null).Concat(deleted).ToArray();

                if (changedItems != null && changedItems.Length > 0)
                {{
                    var lockedItems = _domRepository.{0}.Filter(this.Query(changedItems.Select(item => item.ID)), new {1}());
                    if (lockedItems.Count() > 0)
                        throw new Rhetos.UserException({2}, ""DataStructure:{0},ID:"" + lockedItems.First().ID.ToString(){3});
                }}
            }}
            ",
                info.Source.GetKeyProperties(),
                info.FilterType,
                CsUtility.QuotedString(info.Title),
                ClientMessageTag.Evaluate(info),
                ComparePropertyTag.Evaluate(info));
        }
    }
}
