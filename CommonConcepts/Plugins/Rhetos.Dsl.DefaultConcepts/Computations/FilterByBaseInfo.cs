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
    [ConceptKeyword("FilterByBase")]
    public class FilterByBaseInfo : IValidationConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string Parameter { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            DataStructureInfo baseDataStructure = GetBaseDataStructure(concepts);
            if (baseDataStructure == null)
                throw new DslSyntaxException("Invalid use of " + this.GetUserDescription() + ". Filter's data structure '" + Source.GetKeyProperties() + "' is not an extension of another base class.");
        }

        private DataStructureInfo GetBaseDataStructure(IEnumerable<IConceptInfo> concepts)
        {
            return concepts.OfType<UniqueReferenceInfo>()
                .Where(ci => ci.Extension == Source)
                .Select(ci => ci.Base)
                .SingleOrDefault();
        }
    }

    [Export(typeof(IConceptMacro))]
    public class FilterByBaseMacro : IConceptMacro<FilterByBaseInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(FilterByBaseInfo conceptInfo, IDslModel existingConcepts)
        {
            DataStructureInfo baseDataStructure = GetBaseDataStructure(conceptInfo, existingConcepts);
            if (baseDataStructure == null)
                return null;

            return new IConceptInfo[]
                       {
                           new FilterByInfo
                               {
                                   Source = conceptInfo.Source,
                                   Parameter = conceptInfo.Parameter,
                                   Expression = GetFilterExpression(conceptInfo, baseDataStructure)
                               },
                           new ModuleExternalReferenceInfo
                               {
                                   Module = new ModuleInfo { Name = conceptInfo.Source.Module.Name },
                                   TypeOrAssembly = typeof(DslUtility).AssemblyQualifiedName
                               }
                       };
        }

        private DataStructureInfo GetBaseDataStructure(FilterByBaseInfo conceptInfo, IDslModel concepts)
        {
            return concepts.FindByType<UniqueReferenceInfo>()
                .Where(ci => ci.Extension == conceptInfo.Source)
                .Select(ci => ci.Base)
                .SingleOrDefault();
        }

        private static string GetFilterExpression(FilterByBaseInfo info, DataStructureInfo baseDataStructure)
        {
            return string.Format(@"(repository, parameter) =>
            {{
                var baseRepositiory = repository.{3}.{4};
                Guid[] references = baseRepositiory.Filter(parameter).Select(item => item.ID).ToArray();
                {0}.{1}[] result = repository.{0}.{1}.Filter(references);

                Rhetos.Utilities.Graph.SortByGivenOrder(result, references, item => item.ID);
                return result;
            }}
",
            info.Source.Module.Name,
            info.Source.Name,
            info.Parameter,
            baseDataStructure.Module.Name,
            baseDataStructure.Name,
            CsUtility.QuotedString(info.GetUserDescription()));
        }
    }
}
