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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// An optimized version of AutoCode for large tables.
    /// It stores the latest used code, so it does not need to read the existing records when generating a new code,
    /// but it requires manual initialization the persisted data at initial deployment or import database records.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutoCodeCached")]
    public class AutoCodeCachedInfo : IMacroConcept
    {
        [ConceptKey]
        public ShortStringPropertyInfo Property { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new IConceptInfo[]
            {
                new SystemRequiredInfo { Property = Property },
                CreateUniqueConstraint(),
            };
        }

        virtual protected IConceptInfo CreateUniqueConstraint()
        {
            return new UniqueMultiplePropertiesInfo
            {
                DataStructure = Property.DataStructure,
                PropertyNames = Property.Name
            };
        }
    }
}
