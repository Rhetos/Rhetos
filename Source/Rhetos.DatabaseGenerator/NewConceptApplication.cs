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
using Rhetos.Dsl;
using Rhetos.Utilities;

namespace Rhetos.DatabaseGenerator
{
    public class NewConceptApplication : ConceptApplication
    {
        /// <summary>
        /// Properties DependsOn, CreateQuery, RemoveQuery will be set by <see cref="DatabaseModelGenerator"/>.
        /// Id will be updated later by <see cref="DatabaseGenerator"/>, when matching with the old concepts applications if same.
        /// OldCreationOrder will not be set or used.Is used only in base for old concept applications.
        /// </summary>
        public NewConceptApplication(IConceptInfo conceptInfo, IConceptDatabaseDefinition conceptImplementation)
        {
            ConceptInfo = conceptInfo;
            ConceptInfoTypeName = conceptInfo.GetType().AssemblyQualifiedName;
            ConceptInfoKey = conceptInfo.GetKey();
            ConceptImplementation = conceptImplementation;
            ConceptImplementationType = conceptImplementation.GetType();
            ConceptImplementationTypeName = ConceptImplementationType.AssemblyQualifiedName;
            ConceptImplementationVersion = GetVersionFromAttribute(ConceptImplementationType);
        }

        private static Version GetVersionFromAttribute(Type implementationType)
        {
            ConceptImplementationVersionAttribute versionAttribute = implementationType
                .GetCustomAttributes(typeof(ConceptImplementationVersionAttribute), false)
                .SingleOrDefault()
                as ConceptImplementationVersionAttribute;

            if (versionAttribute != null)
                return versionAttribute.Version;
            return new Version(0, 0);
        }

        public IConceptInfo ConceptInfo;
        public IConceptDatabaseDefinition ConceptImplementation;
        public Type ConceptImplementationType;
        public Version ConceptImplementationVersion;
    }
}
