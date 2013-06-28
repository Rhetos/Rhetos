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
using System.Security.Cryptography;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComputeForNewBaseItemsExtensionInfo))]
    public class ComputeForNewBaseItemsExtensionCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComputeForNewBaseItemsExtensionInfo) conceptInfo;

            var baseDS = info.Extends.Base;
            var persistedExtension = (PersistedDataStructureInfo) info.Extends.Extension;

            string uniqueName = persistedExtension.Module.Name + persistedExtension.Name;
            codeBuilder.InsertCode(RecomputeForNewItemsSnippet(baseDS, persistedExtension, uniqueName, info.FilterSaveExpression), WritableOrmDataStructureCodeGenerator.OnSaveTag1, baseDS);
            codeBuilder.InsertCode(HelperFunctionSnippet(baseDS, uniqueName), RepositoryHelper.RepositoryMembers, baseDS);

            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                codeBuilder.InsertCode(FilterSaveFunction(info, uniqueName), RepositoryHelper.RepositoryMembers, baseDS);
        }

        private static string RecomputeForNewItemsSnippet(DataStructureInfo hookOnSave, PersistedDataStructureInfo updatePersistedComputation, string uniqueName, string filterSaveExpression)
        {
            return string.Format(
@"            if (inserted.Count() > 0)
            {{
                IEnumerable<{0}.{1}> changedItems = inserted;
                var filter = _filterFunctionComputeForNewBaseItems{2}(changedItems);
                _domRepository.{3}.{4}.Recompute(filter{5});

                // Workaround to restore NH proxies after using NHSession.Clear() when saving data in Recompute().
                for (int i=0; i<inserted.Length; i++) inserted[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(inserted[i].ID);
                for (int i=0; i<updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(updated[i].ID);
            }}
",
                hookOnSave.Module.Name,
                hookOnSave.Name,
                uniqueName,
                updatePersistedComputation.Module.Name,
                updatePersistedComputation.Name,
                !string.IsNullOrWhiteSpace(filterSaveExpression) ? (", _filterSaveComputeForNewBaseItems" + uniqueName) : "");
        }

        private static string HelperFunctionSnippet(DataStructureInfo hookOnSave, string uniqueName)
        {
            return string.Format(
@"        private static readonly Func<IEnumerable<{0}.{1}>, Guid[]> _filterFunctionComputeForNewBaseItems{2} =
            changedItems => changedItems.Select(item => item.ID).ToArray();

",
                hookOnSave.Module.Name,
                hookOnSave.Name,
                uniqueName);
        }

        private static string FilterSaveFunction(ComputeForNewBaseItemsExtensionInfo info, string uniqueName)
        {
            return string.Format(
@"        private static readonly Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> _filterSaveComputeForNewBaseItems{2} =
            {3};

",
                info.Extends.Extension.Module.Name, info.Extends.Extension.Name, uniqueName, info.FilterSaveExpression);
        }
    }
}
