/*
    Copyright (C) 2013 Omega software d.o.o.

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
    [ConceptKeyword("PropertyFrom")]
    public class PropertyFromInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Destination { get; set; }

        [ConceptKey]
        public PropertyInfo Source { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
            var property = (PropertyInfo)cloneMethod.Invoke(Source, null);
            property.DataStructure = Destination;
            newConcepts.Add(property);

            //var destinationEntity = Destination as EntityInfo;
            //if (destinationEntity != null)
            //{

            var required = existingConcepts.OfType<RequiredPropertyInfo>().Where(ci => ci.Property == Source)
                .Select(ci => new RequiredPropertyInfo { Property = property })
                .SingleOrDefault();
            if (required != null)
                newConcepts.Add(required);

            var destinationProperties = new HashSet<string>(
                existingConcepts.OfType<PropertyInfo>()
                    .Where(ci => ci.DataStructure == Destination)
                    .Select(ci => ci.Name));

            if (SqlIndexMultipleInfo.IsSupported(Destination))
                foreach (var sourceIndex in existingConcepts.OfType<SqlIndexMultipleInfo>().Where(ci => ci.Entity == Source.DataStructure))
                {
                    var indexProperties = sourceIndex.PropertyNames.Split(' ');
                    if (property.Name == indexProperties.FirstOrDefault()
                        && indexProperties.Skip(1).All(indexProperty => destinationProperties.Contains(indexProperty)))
                        newConcepts.Add(new SqlIndexMultipleInfo { Entity = Destination, PropertyNames = sourceIndex.PropertyNames });
                }

            return newConcepts;
        }
    }
}
