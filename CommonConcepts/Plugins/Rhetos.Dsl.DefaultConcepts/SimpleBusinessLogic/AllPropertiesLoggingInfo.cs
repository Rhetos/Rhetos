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

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllProperties")]
    public class AllPropertiesLoggingInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityLoggingInfo EntityLogging { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class AllPropertiesLoggingMacro : IConceptMacro<AllPropertiesLoggingInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(AllPropertiesLoggingInfo conceptInfo, IDslModel existingConcepts)
        {
            return existingConcepts.FindByReference<PropertyInfo>(prop => prop.DataStructure, conceptInfo.EntityLogging.Entity)
                .Select(prop => new PropertyLoggingInfo
                                    {
                                        EntityLogging = conceptInfo.EntityLogging,
                                        Property = prop
                                    });
        }
    }
}
