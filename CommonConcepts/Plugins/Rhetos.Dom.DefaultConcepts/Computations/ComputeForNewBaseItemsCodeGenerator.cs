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
    [ExportMetadata(MefProvider.Implements, typeof(ComputeForNewBaseItemsInfo))]
    public class ComputeForNewBaseItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComputeForNewBaseItemsInfo) conceptInfo;

            var baseDS = info.Dependency_Extends.Base;
            var persistedExtension = info.EntityComputedFrom.Target;
            var uniqueSuffix = GetUniqueSuffixWithinBase(info);

            codeBuilder.InsertCode(RecomputeForNewItemsSnippet(info, uniqueSuffix), WritableOrmDataStructureCodeGenerator.OnSaveTag1, baseDS);
            codeBuilder.InsertCode(HelperFunctionSnippet(info, uniqueSuffix), RepositoryHelper.RepositoryMembers, baseDS);

            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                codeBuilder.InsertCode(FilterSaveFunction(info, uniqueSuffix), RepositoryHelper.RepositoryMembers, baseDS);
        }

        private static string GetUniqueSuffixWithinBase(ComputeForNewBaseItemsInfo info)
        {
            var baseModule = info.Dependency_Extends.Base.Module;
            return DslUtility.NameOptionalModule(info.EntityComputedFrom.Source, baseModule)
                + DslUtility.NameOptionalModule(info.EntityComputedFrom.Target, baseModule);
        }

        private static string RecomputeForNewItemsSnippet(ComputeForNewBaseItemsInfo info, string uniqueSuffix)
        {
            DataStructureInfo hookOnSave = info.Dependency_Extends.Base;
            EntityInfo updatePersistedComputation = info.EntityComputedFrom.Target;

            return string.Format(
            @"if (inserted.Count() > 0)
            {{
                IEnumerable<{0}.{1}> changedItems = inserted;
                var filter = _filterComputeForNewBaseItems_{2}(changedItems);
                _domRepository.{3}.{4}.{6}(filter{5});
            }}
            ",
                hookOnSave.Module.Name,
                hookOnSave.Name,
                uniqueSuffix,
                updatePersistedComputation.Module.Name,
                updatePersistedComputation.Name,
                !string.IsNullOrWhiteSpace(info.FilterSaveExpression) ? (", _filterSaveComputeForNewBaseItems_" + uniqueSuffix) : "",
                EntityComputedFromCodeGenerator.RecomputeFunctionName(info.EntityComputedFrom));
        }

        private static string HelperFunctionSnippet(ComputeForNewBaseItemsInfo info, string uniqueSuffix)
        {
            DataStructureInfo hookOnSave = info.Dependency_Extends.Base;

            return string.Format(
        @"private static readonly Func<IEnumerable<{0}.{1}>, Guid[]> _filterComputeForNewBaseItems_{2} =
            changedItems => changedItems.Select(item => item.ID).ToArray();

        ",
                hookOnSave.Module.Name,
                hookOnSave.Name,
                uniqueSuffix);
        }

        private static string FilterSaveFunction(ComputeForNewBaseItemsInfo info, string uniqueSuffix)
        {
            return string.Format(
        @"private static readonly Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> _filterSaveComputeForNewBaseItems_{2} =
            {3};

        ",
                info.Dependency_Extends.Extension.Module.Name, info.Dependency_Extends.Extension.Name, uniqueSuffix, info.FilterSaveExpression);
        }
    }
}
