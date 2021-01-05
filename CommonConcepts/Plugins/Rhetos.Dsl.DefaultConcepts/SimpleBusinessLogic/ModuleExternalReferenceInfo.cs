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
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Adds the DLL reference for the generated application. 
    /// The DLL can be referenced in two ways:
    ///     1) (Recommended) By C# type which is used (the assembly qualified name). Version, Culture or PublicKeyToken can be removed from the AssemblyQualifiedName for DLLs that are placed in the Rhetos application folder.
    ///     2) By DLL name(e.g. 'Rhetos.MyFunctions.dll').
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ExternalReference")]
    [Obsolete("Add a NuGet dependency or a project reference to specify Rhetos application dependency to external library.")]
    public class ModuleExternalReferenceInfo : IConceptInfo
    {
        [ConceptKey]
        public ModuleInfo Module { get; set; }

        [ConceptKey]
        public string TypeOrAssembly { get; set; }
    }
}