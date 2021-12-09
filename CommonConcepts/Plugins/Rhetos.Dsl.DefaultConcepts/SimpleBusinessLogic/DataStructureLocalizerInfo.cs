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

using Rhetos.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Adds a standard "_localizer" property to the repository class.
    /// It is a typed localizer (<see cref="ILocalizer{TEntity}"/>) that allows custom property name localization of the given data structure.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("DataStructureLocalizer")]
    public class DataStructureLocalizerInfo : RepositoryUsesInfo, IAlternativeInitializationConcept
    {
        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(PropertyName), nameof(PropertyType) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            PropertyName = "_localizer";
            PropertyType = $"Rhetos.Utilities.ILocalizer<{DataStructure.FullName}>";

            createdConcepts = null;
        }
    }
}
