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
    [ExportMetadata(MefProvider.Implements, typeof(PersistedDataStructureInfo))]
    public class PersistedDataStructureCodeGenerator : IConceptCodeGenerator
    {
        protected static string CodeSnippet(PersistedDataStructureInfo info)
        {
            return string.Format(
        @"public IEnumerable<{0}> Recompute(object filterLoad = null, Func<IEnumerable<{0}>, IEnumerable<{0}>> filterSave = null)
        {{
            return {2}(filterLoad, filterSave);
        }}

        ",
            info.GetKeyProperties(),
            info.Source.GetKeyProperties(),
            EntityComputedFromCodeGenerator.RecomputeFunctionName(new EntityComputedFromInfo { Source = info.Source, Target = info }));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PersistedDataStructureInfo)conceptInfo;
            codeBuilder.InsertCode(CodeSnippet(info), RepositoryHelper.RepositoryMembers, info);
        }
    }
}
