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
    [ExportMetadata(MefProvider.Implements, typeof(KeepSynchronizedOnChangedItemsInfo))]
    public class KeepSynchronizedOnChangedItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeepSynchronizedOnChangedItemsInfo) conceptInfo;

            string uniqueName = (_uniqueNumber++).ToString();

            codeBuilder.InsertCode(
                FilterOldItemsBeforeSaveSnippet(uniqueName, info.UpdateOnChange.FilterType),
                WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.UpdateOnChange.DependsOn);

            codeBuilder.InsertCode(
                FilterAndRecomputeAfterSave(info, uniqueName),
                WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.UpdateOnChange.DependsOn);

            codeBuilder.InsertCode(
                FilterLoadFunction(info.UpdateOnChange.DependsOn, info.UpdateOnChange.FilterType, info.UpdateOnChange.FilterFormula, uniqueName),
                RepositoryHelper.RepositoryMembers, info.UpdateOnChange.DependsOn);

            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                codeBuilder.InsertCode(FilterSaveFunction(info, uniqueName), RepositoryHelper.RepositoryMembers, info.UpdateOnChange.DependsOn);
        }

        private static int _uniqueNumber = 1;

        private static string FilterOldItemsBeforeSaveSnippet(string uniqueName, string fliterType)
        {
            return string.Format(
@"            {1} filterKeepSynchronizedOnChangedItems{0}Old = _filterLoadKeepSynchronizedOnChangedItems{0}(updated.Concat(deleted).ToArray());

",
                uniqueName, fliterType);
        }

        private static string FilterAndRecomputeAfterSave(KeepSynchronizedOnChangedItemsInfo info, string uniqueName)
        {
            return string.Format(
@"            {{
                var filteredNew = _filterLoadKeepSynchronizedOnChangedItems{0}(inserted.Concat(updated).ToArray());
                _domRepository.{1}.{2}.{6}(filterKeepSynchronizedOnChangedItems{0}Old{3});
                _domRepository.{1}.{2}.{6}(filteredNew{3});
                
                // Workaround to restore NH proxies after using NHSession.Clear() when saving data in Recompute().
                for (int i=0; i<inserted.Length; i++) inserted[i] = _executionContext.NHibernateSession.Load<{4}.{5}>(inserted[i].ID);
                for (int i=0; i<updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{4}.{5}>(updated[i].ID);
            }}
",
                uniqueName,
                info.EntityComputedFrom.Target.Module.Name,
                info.EntityComputedFrom.Target.Name,
                !string.IsNullOrWhiteSpace(info.FilterSaveExpression) ? (", _filterSaveKeepSynchronizedOnChangedItems" + uniqueName) : "",
                info.UpdateOnChange.DependsOn.Module.Name,
                info.UpdateOnChange.DependsOn.Name,
                EntityComputedFromInfo.RecomputeFunctionName(info.EntityComputedFrom));
        }

        private static string FilterLoadFunction(DataStructureInfo hookOnSaveEntity, string filterType, string filterFormula, string uniqueName)
        {
            return string.Format(
@"        private static readonly Func<IEnumerable<{0}.{1}>, {2}> _filterLoadKeepSynchronizedOnChangedItems{3} =
            {4};

",
                hookOnSaveEntity.Module.Name,
                hookOnSaveEntity.Name,
                filterType,
                uniqueName,
                filterFormula);
        }

        private static string FilterSaveFunction(KeepSynchronizedOnChangedItemsInfo info, string uniqueName)
        {
            return string.Format(
@"        private static readonly Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> _filterSaveKeepSynchronizedOnChangedItems{2} =
            {3};

",
                info.EntityComputedFrom.Target.Module.Name, info.EntityComputedFrom.Target.Name, uniqueName, info.FilterSaveExpression);
        }
    }
}
