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
        public static readonly CsTag<EntityComputedFromInfo> CompareKeyPropertyTag = "CompareKeyProperty";
        public static readonly CsTag<EntityComputedFromInfo> CompareValuePropertyTag = "CompareValueProperty";
        public static readonly CsTag<EntityComputedFromInfo> ClonePropertyTag = "CloneProperty";
        public static readonly CsTag<EntityComputedFromInfo> AssignPropertyTag = "AssignProperty";
        public static readonly CsTag<EntityComputedFromInfo> OverrideDefaultFiltersTag = new CsTag<EntityComputedFromInfo>("OverrideDefaultFilters", TagType.Reverse);

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityComputedFromInfo)conceptInfo;
            codeBuilder.InsertCode(CodeSnippet(info), RepositoryHelper.RepositoryMembers, info.Target);
        }

        public static string RecomputeFunctionName(EntityComputedFromInfo info)
        {
            return "RecomputeFrom" + DslUtility.NameOptionalModule(info.Source, info.Target.Module);
        }

        private static string CodeSnippet(EntityComputedFromInfo info)
        {
            return string.Format(
@"        private class {2}_KeyComparer : IComparer<{0}>
        {{
            public int Compare({0} x, {0} y)
            {{
                int diff;
                {6}
                return diff;
            }}
        }}

        public IEnumerable<{0}> {2}(object filterLoad = null, Func<IEnumerable<{0}>, IEnumerable<{0}>> filterSave = null)
        {{
            {7}
            filterLoad = filterLoad ?? new FilterAll();
            filterSave = filterSave ?? (x => x);

            var sourceRepository = _executionContext.GenericRepositories.GetGenericRepository<{1}>();
            var sourceItems = sourceRepository.Read(filterLoad);
            var newItems = sourceItems.Select(sourceItem => new {0} {{
                ID = sourceItem.ID,
                {4} }}).ToList();

            var destinationRepository = _executionContext.GenericRepositories.GetGenericRepository<{0}>();
            destinationRepository.InsertOrUpdateOrDelete(
                newItems,
                sameRecord: new {2}_KeyComparer(),
                sameValue: (x, y) =>
                {{
                    {3}
                    return true;
                }},
                filterLoad: filterLoad,
                assign: (destination, source) =>
                {{
                    {5}
                }},
                beforeSave: (ref IEnumerable<{0}> toInsert, ref IEnumerable<{0}> toUpdate, ref IEnumerable<{0}> toDelete) =>
                {{
                    toInsert = filterSave(toInsert);
                    toUpdate = filterSave(toUpdate);
                    toDelete = filterSave(toDelete);
                }});
            return newItems;
        }}

",
            info.Target.GetKeyProperties(),
            info.Source.GetKeyProperties(),
            RecomputeFunctionName(info),
            CompareValuePropertyTag.Evaluate(info),
            ClonePropertyTag.Evaluate(info),
            AssignPropertyTag.Evaluate(info),
            CompareKeyPropertyTag.Evaluate(info),
            OverrideDefaultFiltersTag.Evaluate(info));
        }
    }
}
