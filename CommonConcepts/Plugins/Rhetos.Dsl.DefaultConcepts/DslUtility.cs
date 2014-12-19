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

        /// <summary>
        /// Concatenates module name and data stucture name. Omits module name if it is same as the context module.
        /// </summary>
        public static string NameOptionalModule(DataStructureInfo dataStructure, ModuleInfo contextModule)
        {
            return dataStructure.Module.Name != contextModule.Name ? (dataStructure.Module.Name + dataStructure.Name) : dataStructure.Name;
        }

        /// <summary>
        /// Creates a clone of the given source property and puts it in the given destination data structure.
        /// The clone should not have active behavior (HierarchyInfo and SimpleReferencePropertyInfo becomes a simple ReferencePropertyInfo, for example).
        /// </summary>
        public static PropertyInfo CreatePassiveClone(PropertyInfo source, DataStructureInfo destination)
        {
            if (source is ReferencePropertyInfo)
            {
                return new ReferencePropertyInfo
                {
                    DataStructure = destination,
                    Name = source.Name,
                    Referenced = ((ReferencePropertyInfo)source).Referenced
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

        /// <summary>
        /// Throws a DslSyntaxException if the argument is not a valid identifier.
        /// </summary>
        public static string ValidateIdentifier(string name, IConceptInfo errorContext, string additionalErrorMessage = null)
        {
            string error = CsUtility.GetIdentifierError(name);
            if (error != null)
            {
                if (!string.IsNullOrEmpty(additionalErrorMessage))
                    error = additionalErrorMessage + " " + error;

                throw new DslSyntaxException(errorContext, error);
            }
            return name;
        }

        public static void ValidatePath(DataStructureInfo source, string path, IEnumerable<IConceptInfo> existingConcepts, IConceptInfo errorContext)
        {
            var property = GetPropertyByPath(source, path, new ConceptsListToDslModel(existingConcepts));
            if (property.IsError)
                throw new DslSyntaxException(errorContext, "Invalid path: " + property.Error);
        }

        /// <summary>This is a temporary solution until IValidationConcept.CheckSemantics is upgraded to use IDslModel.</summary>
        private class ConceptsListToDslModel : IDslModel
        {
            public ConceptsListToDslModel(IEnumerable<IConceptInfo> concepts)
            {
                Concepts = concepts;
            }
            public IEnumerable<IConceptInfo> Concepts { get; set; }
            public IConceptInfo FindByKey(string conceptKey)
            {
                throw new NotImplementedException();
            }
            public IEnumerable<IConceptInfo> FindByType(Type conceptType)
            {
                return Concepts.Where(c => conceptType.IsAssignableFrom(c.GetType()));
            }
            public T GetIndex<T>() where T : IDslModelIndex
            {
                throw new NotImplementedException();
            }
        }

        public static PropertyInfo FindProperty(IDslModel dslModel, DataStructureInfo dataStructure, string propertyName)
        {
            if (dslModel is ConceptsListToDslModel)
                return dslModel.Concepts.OfType<PropertyInfo>()
                    .Where(p => p.DataStructure == dataStructure
                        && p.Name == propertyName)
                    .SingleOrDefault();
            else
            {
                var propertyKey = string.Format("PropertyInfo {0}.{1}.{2}", dataStructure.Module.Name, dataStructure.Name, propertyName);
                return (PropertyInfo)dslModel.FindByKey(propertyKey);
            }
        }

        public static ValueOrError<PropertyInfo> GetPropertyByPath(DataStructureInfo source, string path, IDslModel existingConcepts)
        {
            if (path.Contains(" "))
                return ValueOrError.CreateError("The path contains a space character.");

            if (string.IsNullOrEmpty(path))
                return ValueOrError.CreateError("The path is empty.");

            var propertyNames = path.Split('.');
            var referenceNames = propertyNames.Take(propertyNames.Count() - 1).ToArray();
            var lastPropertyName = propertyNames[propertyNames.Count() - 1];

            ValueOrError<DataStructureInfo> selectedDataStructure = source;
            foreach (var referenceName in referenceNames)
            {
                selectedDataStructure = NavigateToNextDataStructure(selectedDataStructure.Value, referenceName, existingConcepts);
                if (selectedDataStructure.IsError)
                    return ValueOrError.CreateError(selectedDataStructure.Error);
            }

            PropertyInfo selectedProperty = FindProperty(existingConcepts, selectedDataStructure.Value, lastPropertyName);

            if (selectedProperty == null && lastPropertyName == "ID")
                return new GuidPropertyInfo { DataStructure = selectedDataStructure.Value, Name = "ID" };

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + lastPropertyName + "' on " + selectedDataStructure.Value.GetUserDescription() + ".");

            return selectedProperty;
        }

        private static ValueOrError<DataStructureInfo> NavigateToNextDataStructure(DataStructureInfo source, string referenceName, IDslModel existingConcepts)
        {
            var selectedProperty = FindProperty(existingConcepts, source, referenceName);

            IEnumerable<DataStructureExtendsInfo> allExtensions;
            if (existingConcepts is ConceptsListToDslModel)
                allExtensions = existingConcepts.Concepts.OfType<DataStructureExtendsInfo>().ToList();
            else
                allExtensions = existingConcepts.FindByType<DataStructureExtendsInfo>();

            if (selectedProperty == null && referenceName == "Base")
            {
                var baseDataStructure = allExtensions
                    .Where(ex => ex.Extension == source)
                    .Select(ex => ex.Base).SingleOrDefault();
                if (baseDataStructure != null)
                    return baseDataStructure;

                if (selectedProperty == null)
                    return ValueOrError.CreateError("There is no property '" + referenceName + "' nor a base data structure on " + source.GetUserDescription() + ".");
            }

            if (selectedProperty == null && referenceName.StartsWith("Extension_"))
            {
                string extensionName = referenceName.Substring("Extension_".Length);
                var extendsionDataStructure = allExtensions
                    .Where(ex => ex.Base == source)
                    .Where(ex => ex.Extension.Module == source.Module && ex.Extension.Name == extensionName
                        || ex.Extension.Module.Name + "_" + ex.Extension.Name == extensionName)
                    .Select(ex => ex.Extension).SingleOrDefault();
                if (extendsionDataStructure != null)
                    return extendsionDataStructure;

                if (selectedProperty == null)
                    return ValueOrError.CreateError("There is no property '" + referenceName + "' nor an extension '" + extensionName + "' on " + source.GetUserDescription() + ".");
            }

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + referenceName + "' on " + source.GetUserDescription() + ".");

            if (!(selectedProperty is ReferencePropertyInfo))
                return ValueOrError.CreateError(string.Format("Property {0} cannot be used in the path because it is '{1}'. Only Reference properties can be used in a path.",
                    selectedProperty.Name, selectedProperty.GetKeywordOrTypeName()));

            return ((ReferencePropertyInfo)selectedProperty).Referenced;
        }
    }
}
