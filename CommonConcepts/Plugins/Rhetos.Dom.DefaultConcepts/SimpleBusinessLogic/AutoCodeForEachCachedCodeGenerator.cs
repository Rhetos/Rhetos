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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodeForEachCachedInfo))]
    public class AutoCodeForEachCachedCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (AutoCodeForEachCachedInfo)conceptInfo;

            string groupSelector;
            if (info.Group is ReferencePropertyInfo)
                groupSelector = info.Group.Name + "ID.ToString()";
            else if (info.Group is ShortStringPropertyInfo || info.Group is LongStringPropertyInfo)
                groupSelector = info.Group.Name;
            else
                groupSelector = info.Group.Name + ".ToString()";

            string groupingSnippet = ", Grouping = item." + groupSelector;
            codeBuilder.InsertCode(groupingSnippet, AutoCodeCachedCodeGenerator.GroupingTag, info);
        }
    }
}
