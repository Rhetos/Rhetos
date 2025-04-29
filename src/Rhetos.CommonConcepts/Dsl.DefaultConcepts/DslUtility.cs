﻿/*
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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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

            if (propertyList.Contains(",") || propertyList.Contains(";"))
                throw new DslSyntaxException(errorHeader.Value + "Property names in the list must be separated with spaces.");

            if (propertyList.Contains("."))
                throw new DslSyntaxException(errorHeader.Value + "The list must contain only property names, without data structure or entity names (or dot character).");

            // Ensure uniqueness of the concept's key. There should not exists different instances of SqlIndexMultiple that generate same index in database.
            if (propertyList.Contains("  "))
                throw new DslSyntaxException(errorHeader.Value + "Property names in the list must be separated by single space character. Verify that there are no multiple spaces.");
            if (propertyList.StartsWith(' '))
                throw new DslSyntaxException(errorHeader.Value + "The list of property names must not start with a space character.");
            if (propertyList.EndsWith(' '))
                throw new DslSyntaxException(errorHeader.Value + "The list of property names must not end with a space character.");
        }

        public static void CheckIfPropertyBelongsToDataStructure(PropertyInfo property, DataStructureInfo dataStructure, IConceptInfo errorContext)
        {
            if (property.DataStructure != dataStructure)
                throw new DslConceptSyntaxException(errorContext,
                    $"Property {property.FullName} is not in data structure {dataStructure.FullName}.");
        }

        /// <summary>
        /// Concatenates module name and data structure name. Omits module name if it is same as the context module.
        /// </summary>
        public static string NameOptionalModule(DataStructureInfo dataStructure, ModuleInfo contextModule)
        {
            return dataStructure.Module.Name != contextModule.Name ? (dataStructure.Module.Name + dataStructure.Name) : dataStructure.Name;
        }

        /// <summary>
        /// Creates a clone of the given source property and puts it in the given destination data structure.
        /// The clone should not have active behavior (<see cref="HierarchyInfo"/> and <see cref="SimpleReferencePropertyInfo"/> becomes a simple <see cref="ReferencePropertyInfo"/>, for example).
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

                throw new DslConceptSyntaxException(errorContext, error);
            }
            return name;
        }

        public static void ValidatePath(DataStructureInfo source, string path, IDslModel existingConcepts, IConceptInfo errorContext)
        {
            var property = GetPropertyByPath(source, path, existingConcepts);
            if (property.IsError)
                throw new DslConceptSyntaxException(errorContext, "Invalid path: " + property.Error);
        }

        public static PropertyInfo FindProperty(IDslModel dslModel, DataStructureInfo dataStructure, string propertyName)
        {
            var propertyKey = string.Format("PropertyInfo {0}.{1}.{2}", dataStructure.Module.Name, dataStructure.Name, propertyName);
            return (PropertyInfo)dslModel.FindByKey(propertyKey);
        }

        /// <param name="allowSystemProperties">
        /// Allows path to end with a C# property that does not have a representation in the DSL model:
        /// 1. The 'ID' property.
        /// 2. The GUID property used for a Reference.
        /// 3. The 'Base' reference property for the extension referencing the base data structure.
        /// </param>
        public static ValueOrError<PropertyInfo> GetPropertyByPath(DataStructureInfo source, string path, IDslModel existingConcepts, bool allowSystemProperties = true)
        {
            if (string.IsNullOrEmpty(path))
                return ValueOrError.CreateError("The path is empty.");

            if (path.Contains(" "))
                return ValueOrError.CreateError("The path contains a space character.");

            var propertyNames = path.Split('.');
            var referenceNames = propertyNames.Take(propertyNames.Length - 1).ToArray();
            var lastPropertyName = propertyNames[propertyNames.Length - 1];

            ValueOrError<DataStructureInfo> selectedDataStructure = source;
            foreach (var referenceName in referenceNames)
            {
                selectedDataStructure = NavigateToNextDataStructure(selectedDataStructure.Value, referenceName, existingConcepts);
                if (selectedDataStructure.IsError)
                    return ValueOrError.CreateError(selectedDataStructure.Error);
            }

            PropertyInfo selectedProperty = FindProperty(existingConcepts, selectedDataStructure.Value, lastPropertyName);

            if (allowSystemProperties && selectedProperty == null && lastPropertyName == "ID")
                return new GuidPropertyInfo { DataStructure = selectedDataStructure.Value, Name = "ID" };

            if (allowSystemProperties && selectedProperty == null && lastPropertyName.EndsWith("ID"))
            {
                string referenceName = lastPropertyName.Substring(0, lastPropertyName.Length - 2);
                var referencePrototype = new PropertyInfo { DataStructure = selectedDataStructure.Value, Name = referenceName };
                if (existingConcepts.FindByKey(referencePrototype.GetKey()) != null)
                    return new GuidPropertyInfo { DataStructure = selectedDataStructure.Value, Name = lastPropertyName };
            }

            if (allowSystemProperties && selectedProperty == null && lastPropertyName == "Base")
            {
                var referenced = NavigateToNextDataStructure(selectedDataStructure.Value, lastPropertyName, existingConcepts);
                if (referenced.IsError)
                    return ValueOrError.CreateError(referenced.Error);
                return new ReferencePropertyInfo { DataStructure = selectedDataStructure.Value, Name = lastPropertyName, Referenced = referenced.Value };
            }

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + lastPropertyName + "' on " + selectedDataStructure.Value.GetUserDescription() + ".");

            return selectedProperty;
        }

        private static ValueOrError<DataStructureInfo> NavigateToNextDataStructure(DataStructureInfo source, string referenceName, IDslModel existingConcepts)
        {
            var selectedProperty = FindProperty(existingConcepts, source, referenceName);

            if (selectedProperty == null && referenceName == "Base")
            {
                var baseDataStructure = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Extension, source)
                    .Select(ex => ex.Base).SingleOrDefault();
                if (baseDataStructure != null)
                    return baseDataStructure;

                return ValueOrError.CreateError("There is no property '" + referenceName + "' nor a base data structure on " + source.GetUserDescription() + ".");
            }

            if (selectedProperty == null && referenceName.StartsWith("Extension_"))
            {
                string extensionName = referenceName.Substring("Extension_".Length);
                var extensionDataStructure = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Base, source)
                    .Where(ex => ex.Extension.Module == source.Module && ex.Extension.Name == extensionName
                        || ex.Extension.Module.Name + "_" + ex.Extension.Name == extensionName)
                    .Select(ex => ex.Extension).SingleOrDefault();
                if (extensionDataStructure != null)
                    return extensionDataStructure;

                return ValueOrError.CreateError("There is no property '" + referenceName + "' nor an extension '" + extensionName + "' on " + source.GetUserDescription() + ".");
            }

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + referenceName + "' on " + source.GetUserDescription() + ".");

            if (!(selectedProperty is ReferencePropertyInfo))
                return ValueOrError.CreateError(string.Format("Property {0} cannot be used in the path because it is '{1}'. Only Reference properties can be used in a path.",
                    selectedProperty.Name, selectedProperty.GetKeywordOrTypeName()));

            return ((ReferencePropertyInfo)selectedProperty).Referenced;
        }

        /// <summary>
        /// Returns a writable data structure that can be used to monitor data changes (intercepting its Save function), in order to update a persisted data.
        /// Returns empty array if a required data structure is not found.
        /// </summary>
        public static IEnumerable<DataStructureInfo> GetBaseChangesOnDependency(DataStructureInfo dependsOn, IDslModel existingConcepts)
        {
            if (dependsOn.Name.EndsWith("_History"))
            {
                var history = existingConcepts.FindByReference<EntityHistoryPropertyInfo>(h => h.Dependency_HistorySqlQueryable, dependsOn)
                    .Select(property => property.Dependency_EntityHistory)
                    .Distinct()
                    .SingleOrDefault();
                if (history != null)
                    return new DataStructureInfo[] { history.Entity, history.Dependency_ChangesEntity };
            }

            if (dependsOn is IWritableOrmDataStructure)
                return new[] { dependsOn };

            if (existingConcepts.FindByReference<WriteInfo>(write => write.DataStructure, dependsOn).Any())
                return new[] { dependsOn };

            var baseDataStructure = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Extension, dependsOn)
                .Select(ex => ex.Base).SingleOrDefault();
            if (baseDataStructure != null)
                return GetBaseChangesOnDependency(baseDataStructure, existingConcepts);

            return Enumerable.Empty<DataStructureInfo>();
        }

        // TODO: Remove this hack after implementing repository concept and cleaner queryable data structure configuration, or use IDataStructureOrmMetadata from ConceptMetadata instead.
        public static bool IsQueryable(DataStructureInfo dataStructure)
        {
            return dataStructure is QueryableExtensionInfo
                || dataStructure is BrowseDataStructureInfo
                || dataStructure is IOrmDataStructure;
        }

        public static IEnumerable<IConceptInfo> CopySqlDependencies(IConceptInfo from, IConceptInfo to, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnDataStructureInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnDataStructureInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnIDInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnIDInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnModuleInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnModuleInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnPropertyInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnPropertyInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlFunctionInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnSqlFunctionInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlIndexInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnSqlIndexInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlObjectInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnSqlObjectInfo { Dependent = to, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlViewInfo>(dep => dep.Dependent, from)
                .Select(dep => new SqlDependsOnSqlViewInfo { Dependent = to, DependsOn = dep.DependsOn }));

            return newConcepts;
        }

        /// <summary>
        /// Simple heuristics for better error detection in legacy features (DeployPackages build references).
        /// Older applications, that used DeployPackages build process instead of Rhetos CLI, needed to use the assembly qualified names,
        /// but it may cause compatibility issues in new code generators where we only need the type name as written in the C# code.
        /// In older applications the <paramref name="typeName"/> was an assembly qualified name,
        /// sometimes without the Version, Culture or PublicKeyToken if referencing a local assembly in the application's folder.
        /// </summary>
        public static void ThrowExceptionIfAssemblyQualifiedName(string typeName, IConceptInfo errorContext)
        {
            var assemblyQualifiedName = _isAssemblyQualifiedNameRegex.Match(typeName);
            if (assemblyQualifiedName.Success)
            {
                string fullName = assemblyQualifiedName.Groups["fullname"].Value;
                fullName = fullName.Replace("+", ".");
                throw new DslConceptSyntaxException(errorContext,
                    "Use a full type name with namespace, as written in C# source, instead of the assembly qualified name." +
                    $" To remove the assembly name from the type name, try '{fullName}' instead of '{typeName}'.");
            }
        }

        /// <summary>
        /// This regex detects if the text uses an assembly qualified name instead of the full type name as written in C# source.
        /// </summary>
        /// <remarks>
        /// False negatives are OK, since its purpose is to provide a more meaningful error to the developer.
        /// It should not have false positives, since it might block a feature implementation.
        /// </remarks>
        private static readonly Regex _isAssemblyQualifiedNameRegex = new Regex(@"(?<fullname>.*?),[^\>\)\]]*$");

        public static (string ResourceKey, string SqlScript) FindSqlResourceKeyPropertyType(this ISqlResources sqlResources, string keyPrefix, PropertyInfo propertyInfo)
            => FindSqlResourceKeyPropertyType(sqlResources, keyPrefix, propertyInfo.GetType());

        public static (string ResourceKey, string SqlScript) FindSqlResourceKeyPropertyType(this ISqlResources sqlResources, string keyPrefix, Type propertyType)
        {
            while (typeof(PropertyInfo).IsAssignableFrom(propertyType))
            {
                string resourceKey = keyPrefix + ConceptInfoHelper.GetKeywordOrTypeName(propertyType);
                string sqlScript = sqlResources.TryGet(resourceKey);
                if (sqlScript != null)
                    return (resourceKey, sqlScript);

                propertyType = propertyType.BaseType;
            }
            return (null, null);
        }
    }
}
