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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ChangesOnLinkedItems")]
    public class ChangesOnLinkedItemsInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Computation { get; set; }

        [ConceptKey]
        public ReferencePropertyInfo LinkedItemsReference { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var extendsConcept = existingConcepts.OfType<DataStructureExtendsInfo>().Where(extends => extends.Extension == Computation).FirstOrDefault();
            if (extendsConcept == null)
                throw new DslSyntaxException("ChangesOnLinkedItems is used on '" + Computation.GetUserDescription()
                    + "' which does not extend another base data structure. Consider adding 'Extends' concept.");

            if (LinkedItemsReference.Referenced != extendsConcept.Base)
                throw new DslSyntaxException("ChangesOnLinkedItems used on '" + Computation.GetUserDescription()
                    + "' declares reference '" + LinkedItemsReference.GetKeyProperties()
                    + "'. The reference should point to computation's base data structure '" + extendsConcept.Base.GetUserDescription()
                    + "'. Instead it points to '" + LinkedItemsReference.Referenced.GetUserDescription() + "'.");

            if (!typeof(EntityInfo).IsAssignableFrom(LinkedItemsReference.DataStructure.GetType()))
                throw new DslSyntaxException("ChangesOnLinkedItems is used on '" + Computation.GetUserDescription()
                + "', but the data structure it depends on '" + LinkedItemsReference.DataStructure.GetUserDescription()
                + "' is not Entity. Currently only entities are supported in automatic handling of dependencies.");

            return new[]
                {
                    new ChangesOnChangedItemsInfo
                        {
                            Computation = Computation,
                            DependsOn = (EntityInfo)LinkedItemsReference.DataStructure,
                            FilterType = "Guid[]",
                            FilterFormula = @"changedItems => changedItems.Where(item => item." + LinkedItemsReference.Name + " != null).Select(item => item." + LinkedItemsReference.Name + ".ID).Distinct().ToArray()"
                        }
                };
        }
    }
}
