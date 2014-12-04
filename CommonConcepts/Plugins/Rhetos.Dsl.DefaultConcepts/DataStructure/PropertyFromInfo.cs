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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("PropertyFrom")]
    public class PropertyFromInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo Destination { get; set; }

        [ConceptKey]
        public PropertyInfo Source { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class PropertyFromMacro : IConceptMacro<PropertyFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(PropertyFromInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var property = DslUtility.CreatePassiveClone(conceptInfo.Source, conceptInfo.Destination);
            newConcepts.Add(property);

            var required = existingConcepts.FindByType<RequiredPropertyInfo>().Where(ci => ci.Property == conceptInfo.Source)
                .Select(ci => new RequiredPropertyInfo { Property = property })
                .SingleOrDefault();
            if (required != null)
                newConcepts.Add(required);

            var destinationProperties = new HashSet<string>(
                existingConcepts.FindByType<PropertyInfo>()
                    .Where(ci => ci.DataStructure == conceptInfo.Destination)
                    .Select(ci => ci.Name));

            if (SqlIndexMultipleInfo.IsSupported(conceptInfo.Destination))
                foreach (var sourceIndex in existingConcepts.FindByType<SqlIndexMultipleInfo>().Where(ci => ci.Entity == conceptInfo.Source.DataStructure))
                {
                    var indexProperties = sourceIndex.PropertyNames.Split(' ');
                    if (property.Name == indexProperties.FirstOrDefault()
                        && indexProperties.Skip(1).All(indexProperty => destinationProperties.Contains(indexProperty)))
                        newConcepts.Add(new SqlIndexMultipleInfo { Entity = conceptInfo.Destination, PropertyNames = sourceIndex.PropertyNames });
                }

            return newConcepts;
        }
    }
}
