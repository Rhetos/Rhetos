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
    [ConceptKeyword("FilterByReferenced")]
    public class FilterByReferencedInfo : IValidatedConcept, IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string Parameter { get; set; }

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

            var availableFilters = existingConcepts.FindByReference<FilterByInfo>(f => f.Source, ReferenceFromMe.Referenced)
                .Select(f => f.Parameter).OrderBy(f => f).ToList();

            if (!availableFilters.Contains(Parameter))
                throw new DslSyntaxException(this, string.Format(
                    "There is no {0} '{1}' on {2}. Available {0} filters are: {3}.",
                    ConceptInfoHelper.GetKeywordOrTypeName(typeof(FilterByInfo)),
                    Parameter,
                    ReferenceFromMe.Referenced.GetUserDescription(),
                    string.Join(", ", availableFilters.Select(parameter => "'" + parameter + "'"))));
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new IConceptInfo[]
                {
                    new FilterByInfo
                    {
                        Source = Source,
                        Parameter = Parameter,
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
            return string.Format(@"(repository, parameter) =>
	        {{
                Guid[] references = repository.{2}.{3}.Filter(parameter).Select(item => item.ID).ToArray();

			    const int BufferSize = 1000;
			    int n = references.Count();
			    var result = new List<{0}.{1}>(n);
			    for (int i = 0; i < (n+BufferSize-1) / BufferSize; i++)
                {{
				    Guid[] idBuffer = references.Skip(i*BufferSize).Take(BufferSize).ToArray();
				    var itemBuffer = repository.{0}.{1}.Query().Where(item => idBuffer.Contains(item.{4}.ID)).ToArray();
				    result.AddRange(itemBuffer);
			    }}

                var groups = result.GroupBy(s => s.{4}ID.Value).ToArray();
                Rhetos.Utilities.Graph.SortByGivenOrder(groups, references, item => item.Key);

                Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> subFilter = {5};
                return groups.SelectMany(g => subFilter(g)).ToArray();
            }}
",
            Source.Module.Name,
            Source.Name,
            ReferenceFromMe.Referenced.Module.Name,
            ReferenceFromMe.Referenced.Name,
            ReferenceFromMe.Name,
            !string.IsNullOrWhiteSpace(SubFilterExpression) ? SubFilterExpression : "items => items");
        }
    }
}
