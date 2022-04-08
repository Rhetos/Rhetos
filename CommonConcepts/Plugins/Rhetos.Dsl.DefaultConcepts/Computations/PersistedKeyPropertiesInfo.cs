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
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// List of properties for matching source and target records. It controls when to update an item or to delete old item and insert a new one.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("KeyProperties")]
    public class PersistedKeyPropertiesInfo : IMacroConcept
    {
        [ConceptKey]
#pragma warning disable CS0618 // Type or member is obsolete. Available for backward compatibility. See PersistedDataStructureInfo.
        public PersistedDataStructureInfo Persisted { get; set; }
#pragma warning restore CS0618 // Type or member is obsolete

        public string KeyProperties { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new[]
            {
                new ComputedFromKeyPropertiesInfo
                {
                    ComputedFrom = Persisted.CreateEntityComputedFrom(),
                    KeyProperties = KeyProperties
                }
            };
        }
    }
}
