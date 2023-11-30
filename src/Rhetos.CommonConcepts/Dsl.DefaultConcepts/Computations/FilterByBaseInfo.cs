﻿/*
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
    /// <summary>
    /// Copies a FilterBy filter from base data structure.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("FilterByBase")]
    public class FilterByBaseInfo : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string Parameter { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DataStructureInfo baseDataStructure = GetBaseDataStructure(existingConcepts);
            if (baseDataStructure == null)
                throw new DslSyntaxException("Invalid use of " + this.GetUserDescription() + ". Filter's data structure '" + Source.GetKeyProperties() + "' is not an extension of another base class.");
        }

        private DataStructureInfo GetBaseDataStructure(IDslModel concepts)
        {
            return concepts.FindByReference<UniqueReferenceInfo>(ci => ci.Extension, Source)
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
                       };
        }

        private DataStructureInfo GetBaseDataStructure(FilterByBaseInfo conceptInfo, IDslModel concepts)
        {
            return concepts.FindByReference<UniqueReferenceInfo>(ci => ci.Extension, conceptInfo.Source)
                .Select(ci => ci.Base)
                .SingleOrDefault();
        }

        private static string GetFilterExpression(FilterByBaseInfo info, DataStructureInfo baseDataStructure)
        {
            return $@"(repository, parameter) =>
            {{
                var baseRepositiory = repository.{baseDataStructure.FullName};
                Guid[] references = baseRepositiory.Load(parameter).Select(item => item.ID).ToArray();
                {info.Source.FullName}[] result = repository.{info.Source.FullName}.Load(references);

                Rhetos.Utilities.Graph.SortByGivenOrder(result, references, item => item.ID);
                return result;
            }}
";
        }
    }
}
