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
    [ExportMetadata(MefProvider.Implements, typeof(HierarchySingleRootInfo))]
    public class HierarchySingleRootInternalCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (HierarchySingleRootInfo)conceptInfo;
            codeBuilder.InsertCode(CheckInvalidItemsSnippet(info), HierarchyInfo.BeforeRecomputeTag, info.Hierarchy);
        }

        private static string CheckInvalidItemsSnippet(HierarchySingleRootInfo info)
        {
            return string.Format(@"if (hierarchyItems.Count(item => item.ParentID == null) > 1)
                        throw new Rhetos.UserException(
                            ""It is not allowed to enter more than one root record in the hierarchy {{0}} by {{1}}."",
                            new[] {{ _localizer[""{0}.{1}""], _localizer[""{2}""] }}, null, null);

                    ",
                info.Hierarchy.DataStructure.Module.Name,
                info.Hierarchy.DataStructure.Name,
                info.Hierarchy.Name);
        }
    }
}
