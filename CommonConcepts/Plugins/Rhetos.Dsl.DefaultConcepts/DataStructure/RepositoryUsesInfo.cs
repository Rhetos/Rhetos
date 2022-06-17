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
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Adds the private C# property to the data structure's repository class. The property value will be resolved from IoC container.
    /// It is typically a system component that is required in some function in the repository class (entity filter or action implementation, e.g.).
    /// 
    /// PropertyType parameter:
    /// It is a C# property type as written in C# source.
    /// It may require using the full name with namespace, if the namespace is not available from repository class or default 'using' statements.
    /// The type will be resolved from IoC container.
    /// </summary>
    /// <remarks>
    /// For older application that uses DeployPackages build process, instead of Rhetos CLI, the property value should be the assembly qualified name,
    /// but it does not need to contain Version, Culture or PublicKeyToken if you are referencing a local assembly in the application's folder.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RepositoryUses")]
    public class RepositoryUsesInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        /// <summary>
        /// Member property name.
        /// </summary>
        [ConceptKey]
        public string PropertyName { get; set; }

        public string PropertyType { get; set; }

        /// <summary>
        /// Simple heuristics for legacy feature activation (DeployPackages build references).
        /// It does not need to detect all cases, since any new code should use C# type syntax.
        /// </summary>
        public bool HasAssemblyQualifiedName() => _isAssemblyQualifiedNameRegex.IsMatch(PropertyType);

        private static readonly Regex _isAssemblyQualifiedNameRegex = new Regex(@",[^\>\)\]]*$");
    }
}
