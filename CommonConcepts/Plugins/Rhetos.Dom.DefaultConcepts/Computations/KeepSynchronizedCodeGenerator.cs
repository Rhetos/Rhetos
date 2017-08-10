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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(KeepSynchronizedInfo))]
    public class KeepSynchronizedCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeepSynchronizedInfo)conceptInfo;

            codeBuilder.InsertCode(FilterSaveFunction(info), RepositoryHelper.RepositoryMembers, info.EntityComputedFrom.Target);
            codeBuilder.InsertCode(SnippetDefaultFilterSaveOnRecompute(info), EntityComputedFromCodeGenerator.OverrideDefaultFiltersTag, info.EntityComputedFrom);
        }

        private static string FilterSaveFunction(KeepSynchronizedInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                return string.Format(
        @"public IEnumerable<{0}.{1}> FilterSaveKeepSynchronizedOnChangedItems_{3}_{4}(IEnumerable<{0}.{1}> filterSave_items)
        {{
            Func<IEnumerable<{0}.{1}>, Common.DomRepository, IEnumerable<{0}.{1}>> filterSaveKeepSynchronizedOnChangedItems_{3}_{4} =
                {2};

            return filterSaveKeepSynchronizedOnChangedItems_{3}_{4}(filterSave_items, _domRepository);
        }}

        ",
                    info.EntityComputedFrom.Target.Module.Name, info.EntityComputedFrom.Target.Name, info.FilterSaveExpression,
                    info.EntityComputedFrom.Source.Module.Name, info.EntityComputedFrom.Source.Name);
            else
                return string.Format(
        @"public IEnumerable<{0}.{1}> FilterSaveKeepSynchronizedOnChangedItems_{3}_{4}(IEnumerable<{0}.{1}> items)
        {{
            return items;
        }}

        ",
                    info.EntityComputedFrom.Target.Module.Name, info.EntityComputedFrom.Target.Name, info.FilterSaveExpression,
                    info.EntityComputedFrom.Source.Module.Name, info.EntityComputedFrom.Source.Name);
        }

        private static string SnippetDefaultFilterSaveOnRecompute(KeepSynchronizedInfo info)
        {
            return string.Format(@"
            filterSave = filterSave ?? FilterSaveKeepSynchronizedOnChangedItems_{3}_{4};",
                info.EntityComputedFrom.Target.Module.Name, info.EntityComputedFrom.Target.Name, info.FilterSaveExpression,
                info.EntityComputedFrom.Source.Module.Name, info.EntityComputedFrom.Source.Name);
        }
    }
}
