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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;

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

            string uniqueName = GetUniqueNameSuffix(info);

            codeBuilder.InsertCode(
                FilterOldItemsBeforeSaveSnippet(info.UpdateOnChange.DependsOn, info.UpdateOnChange.FilterType, info.UpdateOnChange.FilterFormula, uniqueName),
                WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.UpdateOnChange.DependsOn);

            codeBuilder.InsertCode(
                FilterAndRecomputeAfterSave(info, info.UpdateOnChange.FilterType, uniqueName),
                WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.UpdateOnChange.DependsOn);
        }

        private static readonly ConcurrentDictionary<(string, string), int> _uniqueNumberByDependsOnAndTarget = new ConcurrentDictionary<(string, string), int>();

        /// <summary>
        /// The generated variables are placed in "DependsOn" repository. They contain "Target" in the name,
        /// and additional disambiguation is made by additional counter by DependsOn and Target.
        /// </summary>
        private static string GetUniqueNameSuffix(KeepSynchronizedOnChangedItemsInfo info)
        {
            var key = (info.UpdateOnChange.DependsOn.GetKey(), info.KeepSynchronized.EntityComputedFrom.Target.GetKey());
            int uniqueNumber = _uniqueNumberByDependsOnAndTarget.AddOrUpdate(key, 1, (_, oldValue) => oldValue + 1);
            return DslUtility.NameOptionalModule(info.KeepSynchronized.EntityComputedFrom.Target, info.UpdateOnChange.DependsOn.Module)
                + uniqueNumber;
        }

        private static string FilterOldItemsBeforeSaveSnippet(DataStructureInfo hookOnSaveEntity, string filterType, string filterFormula, string uniqueName)
        {
            return
            $@"Func<IEnumerable<Common.Queryable.{hookOnSaveEntity.Module.Name}_{hookOnSaveEntity.Name}>, {filterType}> filterLoadKeepSynchronizedOnChangedItems{uniqueName} =
                {filterFormula};
            {filterType} filterKeepSynchronizedOnChangedItems{uniqueName}Old = filterLoadKeepSynchronizedOnChangedItems{uniqueName}(updated.Concat(deleted));

            ";
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
