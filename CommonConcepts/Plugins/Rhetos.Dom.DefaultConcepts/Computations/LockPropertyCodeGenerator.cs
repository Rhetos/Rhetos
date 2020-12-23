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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LockPropertyInfo))]
    public class LockPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LockPropertyInfo)conceptInfo;
            codeBuilder.InsertCode(CheckLockedPropertySnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Source.DataStructure);
        }

        private static string CheckLockedPropertySnippet(LockPropertyInfo info)
        {
            string propertyName = info.Source.Name;
            if (info.Source is ReferencePropertyInfo)
                propertyName += "ID";

            return string.Format(
            @"if (updatedNew.Count() > 0 || deletedIds.Count() > 0)
            {{
                {0}[] changedItems = updated.Zip(updatedNew, (i, j) => ({4})
                    ? i : null).Where(x => x != null).ToArray();

                if (changedItems != null && changedItems.Length > 0)
                {{
                    var lockedItems = _domRepository.{0}.Filter(this.Query(changedItems.Select(item => item.ID)), new {1}());
                    if (lockedItems.Count() > 0)
                        throw new Rhetos.UserException({2}, ""DataStructure:{0},ID:"" + lockedItems.First().ID.ToString() + "",Property:{3}"");
                }}
            }}
            ",
                info.Source.DataStructure.GetKeyProperties(),
                info.FilterType,
                CsUtility.QuotedString(info.Title),
                propertyName,
                CompareValuePropertySnippet(propertyName));
        }

        private static string CompareValuePropertySnippet(string propertyName)
        {
            return string.Format(
                "i.{0} == null && j.{0} != null || i.{0} != null && !i.{0}.Equals(j.{0})",
                propertyName);
        }
    }
}
