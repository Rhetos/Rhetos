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

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllProperties")]
    public class AllPropertyLoggingInfo : IConceptInfo, IMacroConcept
    {
        [ConceptKey]
        public EntityLoggingInfo EntityLogging { get; set; }

        public override string ToString()
        {
            return "All Property Logging: " + EntityLogging.Entity;
        }

        public override int GetHashCode()
        {
            return EntityLogging.GetHashCode();
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var alreadyLoggedProperties = new HashSet<PropertyInfo>(
                existingConcepts
                    .Where(c => c is PropertyLoggingInfo).Select(c => (PropertyLoggingInfo) c)
                    .Where(propLog => propLog.EntityLogging == EntityLogging)
                    .Select(propLog => propLog.Property));

            return existingConcepts
                .Where(c => c is PropertyInfo).Select(c => (PropertyInfo) c)
                .Where(prop => prop.DataStructure == EntityLogging.Entity)
                .Where(prop => !alreadyLoggedProperties.Contains(prop))
                .Select(prop => new PropertyLoggingInfo
                                    {
                                        EntityLogging = EntityLogging,
                                        Property = prop
                                    })
                .ToList();

        }
    }
}
