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
using System.Reflection;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllPropertiesFrom")]
    public class AllPropertiesFromInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo Destination { get; set; }

        [ConceptKey]
        public DataStructureInfo Source { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class AllPropertiesFromMacro : IConceptMacro<AllPropertiesFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(AllPropertiesFromInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(
                existingConcepts.FindByType<PropertyInfo>().Where(ci => ci.DataStructure == conceptInfo.Source)
                .Select(property => new PropertyFromInfo { Destination = conceptInfo.Destination, Source = property }));

            newConcepts.AddRange(
                existingConcepts.FindByType<UniqueReferenceInfo>().Where(ci => ci.Extension == conceptInfo.Source)
                .Select(ci => ci is DataStructureExtendsInfo
                    ? new DataStructureExtendsInfo { Extension = conceptInfo.Destination, Base = ci.Base }
                    : new UniqueReferenceInfo { Extension = conceptInfo.Destination, Base = ci.Base }));

            return newConcepts;
        }
    }
}
