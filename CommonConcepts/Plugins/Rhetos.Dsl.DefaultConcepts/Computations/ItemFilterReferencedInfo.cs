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
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ItemFilterReferenced")]
    public class ItemFilterReferencedInfo : IValidatedConcept, IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string FilterName { get; set; }

        public ReferencePropertyInfo ReferenceFromMe { get; set; }

        /// <summary>
        /// Use it to additionally filter out some items or sort the items within a group with the same reference value.
        /// </summary>
        public string SubFilterExpression { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (ReferenceFromMe.DataStructure != Source)
                throw new DslSyntaxException("'" + this.GetUserDescription()
                    + "' must use a reference property that is a member of it's own data structure. Try using FilterByLinkedItems instead.");

            var availableFilters = existingConcepts.FindByReference<ItemFilterInfo>(f => f.Source, ReferenceFromMe.Referenced)
                .Select(f => f.FilterName).OrderBy(f => f).ToList();

            if (!availableFilters.Contains(FilterName))
                throw new DslSyntaxException(this, string.Format(
                    "There is no {0} '{1}' on {2}. Available {0} filters are: {3}.",
                    ConceptInfoHelper.GetKeywordOrTypeName(typeof(ItemFilterInfo)),
                    FilterName,
                    ReferenceFromMe.Referenced.GetUserDescription(),
                    string.Join(", ", availableFilters.Select(name => "'" + name + "'"))));
        }

        private string ComposableFilterParameter()
        {
            var itemFilterPrototype = new ItemFilterInfo { Source = Source, FilterName = FilterName, Expression = null };
            return ItemFilterMacro.GetGeneratedFilter(itemFilterPrototype).GetKeyProperties();
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new IConceptInfo[]
                {
                    new ComposableFilterByInfo
                    {
                        Source = Source,
                        Parameter = ComposableFilterParameter(),
                        Expression = GetFilterExpression()
                    },
                    new ModuleExternalReferenceInfo
                    {
                        Module = new ModuleInfo { Name = Source.Module.Name },
                        TypeOrAssembly = typeof(Graph).AssemblyQualifiedName
                    }
                };
        }

        private string GetFilterExpression()
        {
            return string.Format(@"(items, repository, parameter) =>
	        {{
                var filteredReferencedItems = repository.{0}.{1}.Filter(repository.{0}.{1}.Query(), parameter);
                var filteredItems = items.Where(item => filteredReferencedItems.Contains(item.{2}));
                {3}
                return filteredItems;
            }}
",
            ReferenceFromMe.Referenced.Module.Name,
            ReferenceFromMe.Referenced.Name,
            ReferenceFromMe.Name,
            !string.IsNullOrWhiteSpace(SubFilterExpression)
                ? "filteredItems = filteredItems.Where(" + SubFilterExpression + ")"
                : "");
        }
    }
}
