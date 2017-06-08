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
using System.Linq;
using System.Text;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(AlternativeKeyComparerInfo))]
    public class AlternativeKeyComparerCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<AlternativeKeyComparerInfo> CompareKeyPropertyTag = "CompareKeyProperty";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (AlternativeKeyComparerInfo)conceptInfo;

            string targetEntity = info.EntityComputedFrom.Target.GetKeyProperties();
            string recomputeFunctionName = EntityComputedFromCodeGenerator.RecomputeFunctionName(info.EntityComputedFrom);
            string keyComparerName = recomputeFunctionName + "_KeyComparer";
            string keyComparerSnippet =
        $@"private class {keyComparerName} : IComparer<{targetEntity}>
        {{
            public int Compare({targetEntity} x, {targetEntity} y)
            {{
                int diff;
                {CompareKeyPropertyTag.Evaluate(info)}
                return diff;
            }}
        }}

        ";
            codeBuilder.InsertCode(keyComparerSnippet, RepositoryHelper.RepositoryMembers, info.EntityComputedFrom.Target);
            codeBuilder.InsertCode($"new {keyComparerName}(), //", EntityComputedFromCodeGenerator.OverrideKeyComparerTag, info.EntityComputedFrom);
        }
    }
}
