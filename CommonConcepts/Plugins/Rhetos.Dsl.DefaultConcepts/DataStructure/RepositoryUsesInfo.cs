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

    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RepositoryUses")]
    public class RepositoryUsesInfo : IConceptInfo, IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        /// <summary>
        /// Member property name.
        /// </summary>
        [ConceptKey]
        public string PropertyName { get; set; }

        public string PropertyType { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            var assemblyQualifiedName = _isAssemblyQualifiedNameRegex.Match(PropertyType);
            if (assemblyQualifiedName.Success)
            {
                string fullName = assemblyQualifiedName.Groups["fullname"].Value;
                throw new DslSyntaxException(this, $"Use a full class name with namespace, instead of the assembly qualified name. Please remove the assembly name from the RepositoryUses type name: try '{fullName}' instead of '{PropertyType}'.");
            }
        }

        /// <summary>
        /// Simple heuristics for legacy feature (DeployPackages build references).
        /// For older applications that used DeployPackages build process instead of Rhetos CLI, the <see cref="PropertyType"/> was an assembly qualified name,
        /// but it did not need to contain Version, Culture or PublicKeyToken if referencing a local assembly in the application's folder.
        /// </summary>
        private static readonly Regex _isAssemblyQualifiedNameRegex = new Regex(@"(?<fullname>.*?),[^\>\)\]]*$");
    }
}
