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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(HierarchySingleRootInternalInfo))]
    public class HierarchySingleRootInternalCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (HierarchySingleRootInternalInfo)conceptInfo;
            codeBuilder.InsertCode(CheckInvalidItemsSnippet(info), Dsl.DefaultConcepts.HierarchyInfo.BeforeRecomputeTag, info.Hierarchy);
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string CheckInvalidItemsSnippet(HierarchySingleRootInternalInfo info)
        {
            return string.Format(@"if (hierarchyItems.Count(item => item.ParentID == null) > 1)
                        throw new Rhetos.UserException(
                            ""It is not allowed to enter more than one root record in the hierarchy {{0}} by {{1}}."",
                            new[] {{ ""{0}.{1}"", ""{2}"" }}, null, null);

                    ",
                info.Hierarchy.DataStructure.Module.Name,
                info.Hierarchy.DataStructure.Name,
                info.Hierarchy.Name);
        }
    }
}
