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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ItemFilter")]
    public class ItemFilterInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string FilterName { get; set; }

        public string Expression { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            ParameterInfo filterParameter;
            var filterNameElements = FilterName.Split('.');
            if (filterNameElements.Count() == 2)
                filterParameter = new ParameterInfo { Module = new ModuleInfo { Name = filterNameElements[0] }, Name = filterNameElements[1] };
            else
                filterParameter = new ParameterInfo { Module = Source.Module, Name = FilterName };

            if (!existingConcepts.OfType<DataStructureInfo>() // Existing filter parameter does not have to be a ParameterInfo. Any DataStructureInfo is allowed.
                .Any(item => item.Module.Name == filterParameter.Module.Name && item.Name == filterParameter.Name))
                    newConcepts.Add(filterParameter);

            var composableFilter = new ComposableFilterByInfo
            { 
                Source = Source, 
                Parameter = filterParameter.GetKeyProperties(),
                Expression = "(source, repository, parameter) => source.Where(" + Expression + ")"
            };

            newConcepts.Add(composableFilter);

            return newConcepts;
        }
    }
}
