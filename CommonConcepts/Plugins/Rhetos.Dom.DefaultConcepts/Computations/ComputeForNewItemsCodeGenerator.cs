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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComputeForNewItemsInfo))]
    public class ComputeForNewItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComputeForNewItemsInfo) conceptInfo;

            var persistedExtension = info.EntityComputedFrom.Target;
            var uniqueSuffixInTarget = GetUniqueSuffixWithinTarget(info);

            codeBuilder.InsertCode(RecomputeForNewItemsSnippet(info, uniqueSuffixInTarget), WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.EntityComputedFrom.Target);

            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                codeBuilder.InsertCode(FilterSaveFunction(info, uniqueSuffixInTarget), RepositoryHelper.RepositoryMembers, info.EntityComputedFrom.Target);
        }

        private static string GetUniqueSuffixWithinTarget(ComputeForNewItemsInfo info)
        {
            return DslUtility.NameOptionalModule(info.EntityComputedFrom.Source, info.EntityComputedFrom.Target.Module);
        }

        private static string RecomputeForNewItemsSnippet(ComputeForNewItemsInfo info, string uniqueSuffix)
        {
            DataStructureInfo hookOnSave = info.EntityComputedFrom.Target;
            EntityInfo updatePersistedComputation = info.EntityComputedFrom.Target;

            return string.Format(
            @"if (inserted.Count() > 0)
            {{
                var filter = inserted.Select(item => item.ID).ToArray();
                {4}(filter{3});
            }}
            ",
                hookOnSave.Module.Name,
                hookOnSave.Name,
                uniqueSuffix,
                !string.IsNullOrWhiteSpace(info.FilterSaveExpression) ? (", _filterSaveComputeForNewItems_" + uniqueSuffix) : "",
                EntityComputedFromCodeGenerator.RecomputeFunctionName(info.EntityComputedFrom));
        }

        private static string FilterSaveFunction(ComputeForNewItemsInfo info, string uniqueSuffix)
        {
            return string.Format(
        @"private static readonly Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> _filterSaveComputeForNewItems_{2} =
            {3};

        ",
                info.EntityComputedFrom.Target.Module.Name,
                info.EntityComputedFrom.Target.Name,
                uniqueSuffix,
                info.FilterSaveExpression);
        }
    }
}
