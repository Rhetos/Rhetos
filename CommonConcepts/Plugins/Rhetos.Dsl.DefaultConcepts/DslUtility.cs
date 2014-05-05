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
using System.Linq.Expressions;
using System.Text;
using Rhetos.Utilities;
using System.Reflection;

namespace Rhetos.Dsl.DefaultConcepts
{
    public static class DslUtility
    {
        public static void ValidatePropertyListSyntax(string propertyList, IConceptInfo errorContext)
        {
            var errorHeader = new Lazy<string>(() => 
                "Invalid format of list '" + propertyList + "' in '" + errorContext.GetUserDescription() + "': ");

            if (string.IsNullOrWhiteSpace(propertyList))
                throw new DslSyntaxException(errorHeader.Value + "The list is empty.");

            if (propertyList.Contains(',') || propertyList.Contains(';'))
                throw new DslSyntaxException(errorHeader.Value + "Property names in the list must be separated with spaces.");

            if (propertyList.Contains('.'))
                throw new DslSyntaxException(errorHeader.Value + "The list must contain only property names, without data structure or entity names (or dot character).");

            // Ensure uniqueness of the concept's key. There should not exists different instances of SqlIndexMultiple that generate same index in database.
            if (propertyList.Contains("  "))
                throw new DslSyntaxException(errorHeader.Value + "Property names in the list must be separated by single space character. Verify that there are no multiple spaces.");
            if (propertyList.StartsWith(" "))
                throw new DslSyntaxException(errorHeader.Value + "The list of property names must not start with a space character.");
            if (propertyList.EndsWith(" "))
                throw new DslSyntaxException(errorHeader.Value + "The list of property names must not end with a space character.");
        }

        public static void CheckIfPropertyBelongsToDataStructure(PropertyInfo property, DataStructureInfo dataStructure, IConceptInfo errorContext)
        {
            if (property.DataStructure != dataStructure)
                throw new Exception(String.Format(
                    "Invalid use of " + errorContext.GetKeywordOrTypeName() + ": Property {0}.{1}.{2} is not in data structure {3}.{4}.",
                    property.DataStructure.Module.Name,
                    property.DataStructure.Name,
                    property.Name,
                    dataStructure.Module.Name,
                    dataStructure.Name));
        }

        public static string NameOptionalModule(DataStructureInfo dataStructure, ModuleInfo module)
        {
            return dataStructure.Module != module ? (dataStructure.Module.Name + dataStructure.Name) : dataStructure.Name;
        }

        /// <summary>
        /// Creates a clone of the given source property and puts it in the given destination data structure.
        /// The clone should not have active behavior (HierarchyInfo us turned into a simple ReferencePropertyInfo, e.g.).
        /// </summary>
        public static PropertyInfo CreatePassiveClone(PropertyInfo source, DataStructureInfo destination)
        {
            if (source is HierarchyInfo)
            {
                return new ReferencePropertyInfo
                {
                    DataStructure = destination,
                    Name = source.Name,
                    Referenced = ((HierarchyInfo)source).Referenced
                };
            }
            else
            {
                var cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
                var property = (PropertyInfo)cloneMethod.Invoke(source, null);
                property.DataStructure = destination;
                return property;
            }
        }
    }
}
