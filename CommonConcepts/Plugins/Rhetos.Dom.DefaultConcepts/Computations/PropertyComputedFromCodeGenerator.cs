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
    [ExportMetadata(MefProvider.Implements, typeof(PropertyComputedFromInfo))]
    public class PropertyComputedFromCodeGenerator : IConceptCodeGenerator
    {
        private static string ComparePropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(
@"                        if (same && (sourceEnum.Current.{1} == null && destEnum.Current.{0} != null || sourceEnum.Current.{1} != null && !sourceEnum.Current.{1}.Equals(destEnum.Current.{0})))
                            same = false;
",
                info.Target.Name, info.Source.Name);
        }

        private static string ClonePropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(
@",
                                    {0} = sourceEnum.Current.{1}", info.Target.Name, info.Source.Name);
        }

        private static string AssignPropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(@"destEnum.Current.{0} = sourceEnum.Current.{1};
                            ",
                info.Target.Name, info.Source.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PropertyComputedFromInfo) conceptInfo;
            codeBuilder.InsertCode(ComparePropertySnippet(info), EntityComputedFromCodeGenerator.ComparePropertyTag, info.EntityComputedFrom);
            codeBuilder.InsertCode(ClonePropertySnippet(info), EntityComputedFromCodeGenerator.ClonePropertyTag, info.EntityComputedFrom);
            codeBuilder.InsertCode(AssignPropertySnippet(info), EntityComputedFromCodeGenerator.AssignPropertyTag, info.EntityComputedFrom);
        }
    }
}
