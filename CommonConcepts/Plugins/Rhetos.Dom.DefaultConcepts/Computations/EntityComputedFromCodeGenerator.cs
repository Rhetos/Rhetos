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
    [ExportMetadata(MefProvider.Implements, typeof(EntityComputedFromInfo))]
    public class EntityComputedFromCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<EntityComputedFromInfo> OverrideKeyComparerTag = "OverrideKeyComparer";
        public static readonly CsTag<EntityComputedFromInfo> CompareValuePropertyTag = "CompareValueProperty";
        public static readonly CsTag<EntityComputedFromInfo> ClonePropertyTag = "CloneProperty";
        public static readonly CsTag<EntityComputedFromInfo> AssignPropertyTag = "AssignProperty";
        public static readonly CsTag<EntityComputedFromInfo> OverrideDefaultFiltersTag = new CsTag<EntityComputedFromInfo>("OverrideDefaultFilters", TagType.Reverse);

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityComputedFromInfo)conceptInfo;

            string targetEntity = info.Target.GetKeyProperties();
            string recomputeFunctionName = RecomputeFunctionName(info);
            string recomputeFunctionSnippet =
        $@"public IEnumerable<{targetEntity}> {recomputeFunctionName}(object filterLoad = null, Func<IEnumerable<{targetEntity}>, IEnumerable<{targetEntity}>> filterSave = null)
        {{
            {OverrideDefaultFiltersTag.Evaluate(info)}
            filterLoad = filterLoad ?? new FilterAll();
            filterSave = filterSave ?? (x => x);

            var sourceRepository = _executionContext.GenericRepositories.GetGenericRepository<{info.Source.GetKeyProperties()}>();
            var sourceItems = sourceRepository.Load(filterLoad);
            var newItems = sourceItems.Select(sourceItem => new {targetEntity} {{
                ID = sourceItem.ID,
                {ClonePropertyTag.Evaluate(info)} }}).ToList();

            var destinationRepository = _executionContext.GenericRepositories.GetGenericRepository<{targetEntity}>();
            destinationRepository.InsertOrUpdateOrDelete(
                newItems,
                sameRecord: {OverrideKeyComparerTag.Evaluate(info)} null, // Default is comparison by ID.
                sameValue: (x, y) =>
                {{
                    {CompareValuePropertyTag.Evaluate(info)}
                    return true;
                }},
                filterLoad: filterLoad,
                assign: (destination, source) =>
                {{
                    {AssignPropertyTag.Evaluate(info)}
                }},
                beforeSave: (ref IEnumerable<{targetEntity}> toInsert, ref IEnumerable<{targetEntity}> toUpdate, ref IEnumerable<{targetEntity}> toDelete) =>
                {{
                    toInsert = filterSave(toInsert);
                    toUpdate = filterSave(toUpdate);
                    toDelete = filterSave(toDelete);
                }});
            return newItems;
        }}
        
        ";
            codeBuilder.InsertCode(recomputeFunctionSnippet, RepositoryHelper.RepositoryMembers, info.Target);
        }

        public static string RecomputeFunctionName(EntityComputedFromInfo info)
        {
            return "RecomputeFrom" + DslUtility.NameOptionalModule(info.Source, info.Target.Module);
        }
    }
}
