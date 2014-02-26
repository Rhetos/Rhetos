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
    public class FilterByReferencedInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string Parameter { get; set; }

        public ReferencePropertyInfo ReferenceFromMe { get; set; }

        /// <summary>
        /// Use it to additionaly filter out some items or sort the items within a group with the same reference value.
        /// </summary>
        public string SubFilterExpression { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (ReferenceFromMe.DataStructure != Source)
                throw new DslSyntaxException("'" + this.GetUserDescription()
                    + "' must use a reference property that is a member of it's own data structure. Try using FilterByLinkedItems instead.");
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new IConceptInfo[]
                       {
                           new FilterByInfo
                               {
                                   Source = Source,
                                   Parameter = Parameter,
                                   Expression = GetFilterExpression(this)
                               },
                           new ModuleExternalReferenceInfo
                               {
                                   Module = new ModuleInfo {Name = Source.Module.Name},
                                   TypeOrAssembly = typeof (DslUtility).AssemblyQualifiedName
                               }
                       };
        }

        private static string GetFilterExpression(FilterByReferencedInfo info)
        {
            return string.Format(@"(repository, parameter) =>
	        {{
                var baseDataSourceRepositiory = repository.{3}.{4} as IFilterRepository<{2}, {3}.{4}>;
                if (baseDataSourceRepositiory == null)
                {{
                    const string userDescription = {6};
                    throw new Rhetos.UserException(""Invalid use of "" + userDescription + "". Filter's base data source '{3}.{4}' does not implement a filter for '{2}'."");
                }}

                Guid[] references = baseDataSourceRepositiory.Filter(parameter).Select(item => item.ID).ToArray();

			    const int BufferSize = 1000;
			    int n = references.Count();
			    var result = new List<{0}.{1}>(n);
			    for (int i = 0; i < (n+BufferSize-1) / BufferSize; i++) {{
				    Guid[] idBuffer = references.Skip(i*BufferSize).Take(BufferSize).ToArray();
				    var itemBuffer = repository.{0}.{1}.Query().Where(item => idBuffer.Contains(item.{5}.ID)).ToArray();
				    result.AddRange(itemBuffer);
			    }}

                var groups = result.GroupBy(s => s.{5}ID.Value).ToArray();
                Rhetos.Utilities.DirectedGraph.SortByGivenOrder(groups, references, item => item.Key);

                Func<IEnumerable<{0}.{1}>, IEnumerable<{0}.{1}>> subFilter = {7};
                return groups.SelectMany(g => subFilter(g)).ToArray();
            }}
",
            info.Source.Module.Name,
            info.Source.Name,
            info.Parameter,
            info.ReferenceFromMe.Referenced.Module.Name,
            info.ReferenceFromMe.Referenced.Name,
            info.ReferenceFromMe.Name,
            CsUtility.QuotedString(info.GetUserDescription()),
            !string.IsNullOrWhiteSpace(info.SubFilterExpression) ? info.SubFilterExpression : "items => items");
        }
    }
}
