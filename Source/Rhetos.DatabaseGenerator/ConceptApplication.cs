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
    public class ConceptApplication
    {
        public Guid Id;

        public string ConceptInfoTypeName;
        public string ConceptInfoKey;
        public string ConceptImplementationTypeName;

        public string CreateQuery; // SQL query used to create the concept in database.
        public string RemoveQuery; // SQL query used to remove the concept in database.
        public ConceptApplicationDependency[] DependsOn;
        public int OldCreationOrder;

        private string _conceptApplicationKey;
        public string GetConceptApplicationKey()
        {
            if (_conceptApplicationKey == null)
                _conceptApplicationKey = ConceptInfoKey + "/" + GetTypeNameWithoutVersion(ConceptImplementationTypeName);
            return _conceptApplicationKey;
        }

        private string _toString;
        public override string ToString()
        {
            if (_toString == null)
                _toString = ConceptInfoKey + "/" + GetUserFriendlyTypeName(ConceptImplementationTypeName);
            return _toString;
        }

        private static string GetTypeNameWithoutVersion(string assemblyQualifiedName)
        {
            return assemblyQualifiedName.Substring(0, assemblyQualifiedName.IndexOf(','));
        }

        private static string GetUserFriendlyTypeName(string assemblyQualifiedName)
        {
            var fullTypeName = GetTypeNameWithoutVersion(assemblyQualifiedName);
            return fullTypeName.Substring(fullTypeName.LastIndexOf('.') + 1); // Works even for type without namespace.
        }
    }
}
