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
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Data structure that represents a simple read-only entity or a code table.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Hardcoded")]
    public class HardcodedEntityInfo : EntityInfo, IMacroConcept
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            yield return new ShortStringPropertyInfo { DataStructure = this, Name = "Name" };
            yield return new DataStructureLocalizerInfo { DataStructure = this };
        }
    }
}
