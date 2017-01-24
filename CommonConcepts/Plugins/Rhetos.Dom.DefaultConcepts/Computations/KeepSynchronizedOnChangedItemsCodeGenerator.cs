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
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.Collections;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(KeepSynchronizedOnChangedItemsInfo))]
    public class KeepSynchronizedOnChangedItemsCodeGenerator : IConceptCodeGenerator
    {
        public static string OverrideRecomputeTag(KeepSynchronizedOnChangedItemsInfo info)
        {
            return string.Format("/*OverrideRecompute {0}.{1}*/",
                info.KeepSynchronized.GetKey(),
                info.UpdateOnChange.GetAlternativeKey());
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeepSynchronizedOnChangedItemsInfo) conceptInfo;

            string uniqueName = (_uniqueNumber++).ToString();

            codeBuilder.InsertCode(
                FilterOldItemsBeforeSaveSnippet(info.UpdateOnChange.DependsOn, info.UpdateOnChange.FilterType, info.UpdateOnChange.FilterFormula, uniqueName),
                WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.UpdateOnChange.DependsOn);

            codeBuilder.InsertCode(
                FilterAndRecomputeAfterSave(info, info.UpdateOnChange.FilterType, uniqueName),
                WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.UpdateOnChange.DependsOn);
        }

        private static int _uniqueNumber = 1;

        private static string FilterOldItemsBeforeSaveSnippet(DataStructureInfo hookOnSaveEntity, string filterType, string filterFormula, string uniqueName)
        {
            return string.Format(
            @"Func<IEnumerable<Common.Queryable.{0}_{1}>, {2}> filterLoadKeepSynchronizedOnChangedItems{3} =
                {4};
            {2} filterKeepSynchronizedOnChangedItems{3}Old = filterLoadKeepSynchronizedOnChangedItems{3}(updated.Concat(deleted));

            ",
                hookOnSaveEntity.Module.Name,
                hookOnSaveEntity.Name,
                filterType,
                uniqueName,
                filterFormula);
        }

        private static string FilterAndRecomputeAfterSave(KeepSynchronizedOnChangedItemsInfo info, string filterType, string uniqueName)
        {
            string recomputeMethodName =
                info.KeepSynchronized.EntityComputedFrom.Target.Module.Name
                + "." + info.KeepSynchronized.EntityComputedFrom.Target.Name
                + "." + EntityComputedFromCodeGenerator.RecomputeFunctionName(info.KeepSynchronized.EntityComputedFrom);

            return
                $@"{OverrideRecomputeTag(info)}
                {{
                    var filteredNew = filterLoadKeepSynchronizedOnChangedItems{uniqueName}(inserted.Concat(updated));
                    {filterType} optimizedFilter;
                    if (KeepSynchronizedHelper.OptimizeFiltersUnion(filteredNew, filterKeepSynchronizedOnChangedItems{uniqueName}Old, out optimizedFilter))
                        _domRepository.{recomputeMethodName}(optimizedFilter);
                    else
                    {{
                        _domRepository.{recomputeMethodName}(filterKeepSynchronizedOnChangedItems{uniqueName}Old);
                        _domRepository.{recomputeMethodName}(filteredNew);
                    }}
                }}

                ";
        }
    }
}
