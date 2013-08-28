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
    [ExportMetadata(MefProvider.Implements, typeof(LockPropertyInfo))]
    public class LockPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LockPropertyInfo)conceptInfo;
            codeBuilder.InsertCode(CheckLockedPropertySnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Source.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string CheckLockedPropertySnippet(LockPropertyInfo info)
        {
            var propName = info.Source.Name;
            if (info.Source is ReferencePropertyInfo)
            {
                propName += "ID";
            }
            string propertyChanged = @"i." + propName + "==null && j." + propName + "!=null || " +
                                     @"i." + propName + "!=null && j." + propName + "==null || " +
                                     @"i." + propName + "!=null && j." + propName + "!=null && !i." + propName + ".Equals(j." + propName + ")";
            return string.Format(
@"            if (updated.Length > 0 || deleted.Length > 0)
            {{
                {0}[] changedItems = updated.Zip(updatedNew, (i, j) => (" + propertyChanged + @")?i:null).Where(x => x != null).ToArray();
                var lockedItems = _domRepository.{0}.Filter(changedItems.AsQueryable(), new {1}());
                if (lockedItems.Count() > 0)
                    throw new Rhetos.UserException({2}, ""DataStructure:{0},ID:"" + lockedItems.First().ID.ToString() + "",Property:{3}"");
            }}
",
                info.Source.DataStructure.GetKeyProperties(),
                info.FilterType,
                CsUtility.QuotedString(info.Title),
                propName);
        }
    }
}
