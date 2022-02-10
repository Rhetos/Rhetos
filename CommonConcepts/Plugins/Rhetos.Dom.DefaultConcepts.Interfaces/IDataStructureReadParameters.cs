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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// If set, the result will include alternative simplified type names without default namespaces,
        /// and alternative array parameter types for IEnumerable types.
        /// For example "Guid[]" will be automatically added for standard filter type "System.Collections.Generic.IEnumerable&lt;System.Guid&gt;".
        /// </param>
        /// <remarks>
        /// In the resulting list, there may be multiple occurrences of same Type.
        /// </remarks>
        IEnumerable<DataStructureReadParameter> GetReadParameters(string dataStuctureFullName, bool extendedSet);
    }
}
