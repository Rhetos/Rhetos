/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Take")]
    public class BrowseTakePropertyInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public BrowseDataStructureInfo Browse { get; set; }

        [ConceptKey]
        public string Path { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            // This is also used in BrowseTakeNamedPropertyInfo.
            return CreateNewConcepts(Browse, Path, Path.Replace(".", ""), existingConcepts, this);
        }

        internal static IEnumerable<IConceptInfo> CreateNewConcepts(
            BrowseDataStructureInfo browse, string path, string newPropertyName,
            IEnumerable<IConceptInfo> existingConcepts, IConceptInfo errorContext)
        {
            var newConcepts = new List<IConceptInfo>();

            ValueOrError<PropertyInfo> sourceProperty = GetSelectedPropertyByPath(browse, path, existingConcepts);
            if (sourceProperty.IsError)
                return null; // Creating the browse property may be delayed for other macro concepts to generate the needed properties. If this condition is not resolved, the CheckSemantics function below will throw an exception.

            if (!IsValidIdentifier(newPropertyName))
                throw new DslSyntaxException(string.Format(
                    "Invalid format of {0}: Property name '{1}' is not a valid identifier. Specify a valid name before the path to override the generated name.",
                    errorContext.GetUserDescription(),
                    newPropertyName));

            var cloneMethod = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var browseProperty = (PropertyInfo)cloneMethod.Invoke(sourceProperty.Value, null);
            browseProperty.DataStructure = browse;
            browseProperty.Name = newPropertyName;

            var browsePropertySelector = new BrowseFromPropertyInfo { PropertyInfo = browseProperty, Path = path };

            return new IConceptInfo[] { browseProperty, browsePropertySelector };
        }

        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if ((name[0] < 'a' || name[0] > 'z') && (name[0] < 'A' || name[0] > 'Z') && name[0] != '_')
                return false;

            foreach (char c in name)
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_')
                    return false;

            return true;
        }

        private static ValueOrError<PropertyInfo> GetSelectedPropertyByPath(BrowseDataStructureInfo browse, string path, IEnumerable<IConceptInfo> existingConcepts)
        {
            if (path.Contains(" "))
                return ValueOrError.CreateError("The path contains a space character.");

            if (string.IsNullOrEmpty(path))
                return ValueOrError.CreateError("The path is empty.");

            var propertyNames = path.Split('.');
            var referenceNames = propertyNames.Take(propertyNames.Count() - 1).ToArray();
            var lastPropertyName = propertyNames[propertyNames.Count() - 1];

            ValueOrError<DataStructureInfo> selectedDataStructure = browse.Source;
            foreach (var referenceName in referenceNames)
            {
                selectedDataStructure = NavigateToNextDataStructure(selectedDataStructure.Value, referenceName, existingConcepts);
                if (selectedDataStructure.IsError)
                    return ValueOrError.CreateError(selectedDataStructure.Error);
            }

            PropertyInfo selectedProperty = existingConcepts.OfType<PropertyInfo>()
                .Where(p => p.DataStructure == selectedDataStructure.Value && p.Name == lastPropertyName)
                .SingleOrDefault();

            if (selectedProperty == null && lastPropertyName == "ID")
                return new GuidPropertyInfo { DataStructure = selectedDataStructure.Value, Name = "ID" };

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + lastPropertyName + "' on " + selectedDataStructure.Value.GetUserDescription() + ".");

            return selectedProperty;
        }

        private static ValueOrError<DataStructureInfo> NavigateToNextDataStructure(DataStructureInfo dataStructure, string referenceName, IEnumerable<IConceptInfo> existingConcepts)
        {
            var selectedProperty = existingConcepts.OfType<PropertyInfo>()
                .Where(p => p.DataStructure == dataStructure && p.Name == referenceName)
                .SingleOrDefault();

            if (selectedProperty == null && referenceName == "Base")
            {
                var baseDataStructure = existingConcepts.OfType<DataStructureExtendsInfo>()
                    .Where(ex => ex.Extension == dataStructure)
                    .Select(ex => ex.Base).SingleOrDefault();
                if (baseDataStructure != null)
                    return baseDataStructure;

                if (selectedProperty == null)
                    return ValueOrError.CreateError("There is no property '" + referenceName + "' nor a base data structure on " + dataStructure.GetUserDescription() + ".");
            }

            if (selectedProperty == null && referenceName.StartsWith("Extension_"))
            {
                string extensionName = referenceName.Substring("Extension_".Length);
                var extendsionDataStructure = existingConcepts.OfType<DataStructureExtendsInfo>()
                    .Where(ex => ex.Base == dataStructure)
                    .Where(ex => ex.Extension.Module == dataStructure.Module && ex.Extension.Name == extensionName
                        || ex.Extension.Module.Name + "_" + ex.Extension.Name == extensionName)
                    .Select(ex => ex.Extension).SingleOrDefault();
                if (extendsionDataStructure != null)
                    return extendsionDataStructure;

                if (selectedProperty == null)
                    return ValueOrError.CreateError("There is no property '" + referenceName + "' nor an extension '" + extensionName + "' on " + dataStructure.GetUserDescription() + ".");
            }

            if (selectedProperty == null)
                return ValueOrError.CreateError("There is no property '" + referenceName + "' on " + dataStructure.GetUserDescription() + ".");

            if (!(selectedProperty is ReferencePropertyInfo))
                return ValueOrError.CreateError(string.Format("Property {0} cannot be used in the path because it is '{1}'. Only Reference properties can be used in a path.",
                    selectedProperty.Name, selectedProperty.GetKeywordOrTypeName()));

            return ((ReferencePropertyInfo)selectedProperty).Referenced;
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            // This is also used in BrowseTakeNamedPropertyInfo.
            CheckSemantics(Browse, Path, concepts, this);
        }

        internal static void CheckSemantics(BrowseDataStructureInfo browse, string path, IEnumerable<IConceptInfo> concepts, IConceptInfo errorContext)
        {
            var property = GetSelectedPropertyByPath(browse, path, concepts);
            if (property.IsError)
                throw new DslSyntaxException("Invalid format of " + errorContext.GetUserDescription() + ": " + property.Error);
        }
    }
}
