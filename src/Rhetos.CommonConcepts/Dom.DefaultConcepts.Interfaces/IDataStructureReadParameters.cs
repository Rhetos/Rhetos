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

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// A helper for external API implementations (web API, e.g.), that provides the available filter types
    /// and other read parameters on a given readable data structure.
    /// </summary>
    public interface IDataStructureReadParameters
    {
        /// <summary>
        /// Returns available filter types and other read parameters on a given readable data structure.
        /// It includes filters specified in DSL scripts, for example with ItemFilter concept, and
        /// other filters that are automatically available on certain data structures.
        /// </summary>
        /// <param name="dataStuctureFullName">
        /// Format "{ModuleName}.{DataStructureName}".
        /// </param>
        /// <param name="extendedSet">
        /// Read parameter types (<see cref="DataStructureReadParameter.Name"/>) are usually specified in C# format,
        /// to be inserted in the generated C# source code.
        /// If <paramref name="extendedSet"/> is <see langword="true"/>, the method result will additionally include alternative type names:
        /// <list type="number">
        /// <item>Type name as provided by <see cref="Type.ToString()"/> (for example "System.Collections.Generic.IEnumerable`1[System.Guid]")</item>
        /// <item>Simplified type name without default namespaces. Simplified type for IEnumerable is additionally converted to array (for example "Guid[]")</item>
        /// </list>
        /// </param>
        /// <remarks>
        /// In the resulting list, there may be multiple occurrences of same Type with different name, for example both "string" and "System.String".
        /// </remarks>
        IEnumerable<DataStructureReadParameter> GetReadParameters(string dataStuctureFullName, bool extendedSet);
    }
}
