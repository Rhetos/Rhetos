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
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Is")]
    public class IsSubtypeOfInfo : IConceptInfo, IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo Subtype { get; set; }

        [ConceptKey]
        public PolymorphicInfo Supertype { get; set; }

        /// <summary>
        /// The same Subtype data structure may implement the same Supertype, using a different ImplementationName.
        /// If there is only one implementation, use empty ImplementationName for better performace.
        /// </summary>
        [ConceptKey]
        public string ImplementationName { get; set; }

        public SqlViewInfo GetImplementationViewPrototype()
        {
            string viewName = Subtype.Name + "_As_" + DslUtility.NameOptionalModule(Supertype, Subtype.Module);
            if (ImplementationName != "")
                viewName += "_" + ImplementationName;
            return new SqlViewInfo { Module = Subtype.Module, Name = viewName, ViewSource = "<prototype>" };
        }

        public string GetSubtypeReferenceName()
        {
            return DslUtility.NameOptionalModule(Subtype, Supertype.Module) + ImplementationName;
        }

        public bool SupportsPersistedSubtypeImplementationColum()
        {
            return !string.IsNullOrEmpty(ImplementationName) && Subtype is EntityInfo;
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!(Subtype is IOrmDataStructure))
                throw new DslSyntaxException(this, "Is (polymorphic) may only be used on a database-mapped data structure, such as Entity or SqlQueryable. "
                    + this.Subtype.GetUserDescription() + " is not IOrmDataStructure.");

            if (ImplementationName == null)
                throw new DslSyntaxException(this, "ImplementationName must not be null. It is allowed to be an empty string.");

            if (!string.IsNullOrEmpty(ImplementationName))
                DslUtility.ValidateIdentifier(ImplementationName, this, "Invalid ImplementationName value.");

            if (existingConcepts.FindByReference<PolymorphicMaterializedInfo>(pm => pm.Polymorphic, Supertype).Any())
            {
                // Verifying if the ChangesOnChangedItemsInfo can be created (see IsSubtypeOfMacro)
                DataStructureInfo dependsOn = DslUtility.GetBaseChangesOnDependency(Subtype, existingConcepts);
                if (dependsOn == null)
                    throw new DslSyntaxException(this, Subtype.GetUserDescription() + " should be an *extension* of an entity. Otherwise it cannot be used in a materialized polymorphic entity because the system cannot detect when to update the persisted data.");
            }
        }
    }

    [Export(typeof(IConceptMacro))]
    public class IsSubtypeOfMacro : IConceptMacro<IsSubtypeOfInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(IsSubtypeOfInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Add a subtype reference (for each subtype) to the supertype data structure:

            var subtypeReference = new ReferencePropertyInfo
            {
                DataStructure = conceptInfo.Supertype,
                Referenced = conceptInfo.Subtype,
                Name = conceptInfo.GetSubtypeReferenceName()
            };
            newConcepts.Add(subtypeReference);
            newConcepts.Add(new PolymorphicPropertyInfo { Property = subtypeReference, SubtypeReference = conceptInfo.Subtype.GetKeyProperties() });

            // Append subtype implementation to the supertype union:

            newConcepts.Add(new SubtypeExtendPolymorphicInfo
            {
                IsSubtypeOf = conceptInfo,
                SubtypeImplementationView = conceptInfo.GetImplementationViewPrototype(),
                PolymorphicUnionView = conceptInfo.Supertype.GetUnionViewPrototype()
            });

            // Add metadata for supertype computation (union):

            if (existingConcepts.FindByReference<PolymorphicMaterializedInfo>(pm => pm.Polymorphic, conceptInfo.Supertype).Any())
            {
                string materializedUpdateSelector;
                if (conceptInfo.ImplementationName == "")
                    materializedUpdateSelector = "changedItems => changedItems.Select(item => item.ID).ToArray()";
                else
                    materializedUpdateSelector = string.Format(
                        @"changedItems => changedItems.Select(item => DomUtility.GetSubtypeImplementationId(item.ID, {0})).ToArray()",
                        DomUtility.GetSubtypeImplementationHash(conceptInfo.ImplementationName));

                DataStructureInfo dependsOn = DslUtility.GetBaseChangesOnDependency(conceptInfo.Subtype, existingConcepts);
                if (dependsOn != null) // The dependent data structure may be created in a later macro iterations. The end result will be check by IValidatedConcept.
                    newConcepts.Add(new ChangesOnChangedItemsInfo
                    {
                        Computation = conceptInfo.Supertype,
                        DependsOn = dependsOn,
                        FilterType = "System.Guid[]",
                        FilterFormula = materializedUpdateSelector
                    });
            }

            // Add metadata for subtype implementation:

            PersistedSubtypeImplementationIdInfo subtypeImplementationColumn = null;
            if (conceptInfo.SupportsPersistedSubtypeImplementationColum())
            {
                subtypeImplementationColumn = new PersistedSubtypeImplementationIdInfo { Subtype = conceptInfo.Subtype, ImplementationName = conceptInfo.ImplementationName };
                newConcepts.Add(subtypeImplementationColumn);
            }

            // Automatic interface implementation:

            if (existingConcepts.FindByKey(conceptInfo.GetImplementationViewPrototype().GetKey()) == null)
            {
                var extensibleSubtypeSqlView = new ExtensibleSubtypeSqlViewInfo { IsSubtypeOf = conceptInfo };
                newConcepts.Add(extensibleSubtypeSqlView);

                if (subtypeImplementationColumn != null)
                    newConcepts.Add(new SqlDependsOnSqlObjectInfo
                    {
                        // The subtype implementation view will use the PersistedSubtypeImplementationColumn.
                        DependsOn = subtypeImplementationColumn.GetSqlObjectPrototype(),
                        Dependent = extensibleSubtypeSqlView
                    });
            }

            return newConcepts;
        }
    }
}
