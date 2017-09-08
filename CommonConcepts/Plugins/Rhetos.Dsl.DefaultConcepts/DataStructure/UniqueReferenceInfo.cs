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
    /// <summary>
    /// The extension data structure has the same ID as the base data structure.
    /// Database:
    /// The extension's table has a foreign key constraint on its ID column, referencing the base entity's ID column.
    /// C# object model:
    /// The extension's class has 'Base' navigation property that references the base class. The base class has Extension_* navigation property that references the extension.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("UniqueReference")]
    public class UniqueReferenceInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo Extension { get; set; }

        public DataStructureInfo Base { get; set; }
  
        public string ExtensionPropertyName()
        {
            if (Base.Module == Extension.Module)
                return "Extension_" + Extension.Name;
            return "Extension_" + Extension.Module.Name + "_" + Extension.Name;
        }
    }
}
