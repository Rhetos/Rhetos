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
    [ConceptKeyword("AllPropertiesWithCascadeDeleteFrom")]
    public class AllPropertiesWithCascadeDeleteFromInfo : AllPropertiesFromInfo
    {
    }

    [Export(typeof(IConceptMacro))]
    [ConceptKeyword("AllPropertiesWithCascadeDeleteFrom")]
    public class AllPropertiesWithCascadeDeleteFromMacro : IConceptMacro<AllPropertiesWithCascadeDeleteFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(AllPropertiesWithCascadeDeleteFromInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();
            
            newConcepts.AddRange(existingConcepts.FindByType<ReferenceCascadeDeleteInfo>().Where(ci => ci.Reference.DataStructure == conceptInfo.Source)
                .Select(ci => new ReferenceCascadeDeleteInfo
                {
                    Reference = new ReferencePropertyInfo
                    {
                        DataStructure = conceptInfo.Destination,
                        Name = ci.Reference.Name
                    }
                }));

            newConcepts.AddRange(existingConcepts.FindByType<UniqueReferenceCascadeDeleteInfo>().Where(ci => ci.UniqueReference.Extension == conceptInfo.Source)
                .Select(ci => new UniqueReferenceCascadeDeleteInfo
                {
                    UniqueReference = new UniqueReferenceInfo
                    {
                        Extension = conceptInfo.Destination
                    }
                }));

            return newConcepts;
        }
    }
}
