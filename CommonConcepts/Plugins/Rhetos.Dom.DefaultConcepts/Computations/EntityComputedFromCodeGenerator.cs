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
        public static readonly CsTag<EntityComputedFromInfo> OverrideDefaultLoadFilterTag = new CsTag<EntityComputedFromInfo>("OverrideDefaultLoadFilter", TagType.Reverse);
        public static readonly CsTag<EntityComputedFromInfo> OverrideDefaultSaveFilterTag = new CsTag<EntityComputedFromInfo>("OverrideDefaultSaveFilter", TagType.Reverse);

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityComputedFromInfo)conceptInfo;

            string diffFunctionName = info.DiffFunctionName();
            string recomputeFunctionName = info.RecomputeFunctionName();

            string repositoryMethods =
        $@"public DiffResult<{info.Target.FullName}> {diffFunctionName}(object filterLoad = null)
        {{
            {OverrideDefaultLoadFilterTag.Evaluate(info)}
            filterLoad = filterLoad ?? new FilterAll();

            return ComputedFromHelper.Diff(
                this, _domRepository.{info.Source.FullName}, _executionContext.LogProvider,
                filterLoad,
                mapping: sourceItem => new {info.Target.FullName}
                {{
                    ID = sourceItem.ID,
                    {ClonePropertyTag.Evaluate(info)}
                }},
                sameRecord: {OverrideKeyComparerTag.Evaluate(info)} null, // Default is comparison by ID.
                sameValue: (x, y) =>
                {{
                    {CompareValuePropertyTag.Evaluate(info)}
                    return true;
                }});
        }}

        public IEnumerable<{info.Target.FullName}> {recomputeFunctionName}(object filterLoad = null, Func<IEnumerable<{info.Target.FullName}>, IEnumerable<{info.Target.FullName}>> filterSave = null)
        {{
            {OverrideDefaultSaveFilterTag.Evaluate(info)}

            var diff = {diffFunctionName}(filterLoad);
            ComputedFromHelper.InsertOrUpdateOrDelete(
                this, _executionContext.LogProvider, diff,
                assign: (destination, source) =>
                {{
                    {AssignPropertyTag.Evaluate(info)}
                }},
                filterSave);

            return diff.NewItems;
        }}
        
        ";
            codeBuilder.InsertCode(repositoryMethods, RepositoryHelper.RepositoryMembers, info.Target);
        }

        [Obsolete("Use EntityComputedFromInfo.RecomputeFunctionName() instead.")]
        public static string RecomputeFunctionName(EntityComputedFromInfo info) => info.RecomputeFunctionName();
    }
}
