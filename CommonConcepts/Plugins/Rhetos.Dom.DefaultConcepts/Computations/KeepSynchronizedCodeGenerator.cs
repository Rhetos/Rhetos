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
            var source = info.EntityComputedFrom.Source;
            var target = info.EntityComputedFrom.Target;

            string filterSaveFunctionName = $"FilterSaveKeepSynchronizedOnChangedItems_{source.Module.Name}_{source.Name}";

            string filterSaveFunctionBody;
            if (string.IsNullOrWhiteSpace(info.FilterSaveExpression))
                filterSaveFunctionBody = $@"return filterSave_items;";
            else
                filterSaveFunctionBody =
            $@"Func<IEnumerable<{target.FullName}>, Common.DomRepository, IEnumerable<{target.FullName}>> filterSaveKeepSynchronizedOnChangedItems =
                {info.FilterSaveExpression};

            return filterSaveKeepSynchronizedOnChangedItems(filterSave_items, _domRepository);";

            string filterSaveFunction = $@"public IEnumerable<{target.Module.Name}.{target.Name}> {filterSaveFunctionName}(IEnumerable<{target.Module.Name}.{target.Name}> filterSave_items)
        {{
            {filterSaveFunctionBody}
        }}

        ";

            codeBuilder.InsertCode(filterSaveFunction, RepositoryHelper.RepositoryMembers, target);

            string overrideDefaultFilters = $@"
            filterSave = filterSave ?? {filterSaveFunctionName};";

            codeBuilder.InsertCode(overrideDefaultFilters, EntityComputedFromCodeGenerator.OverrideDefaultFiltersTag, info.EntityComputedFrom);
        }
    }
}
