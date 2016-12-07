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
        /// If there is only one implementation, use empty ImplementationName for better performance.
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
                var dependsOn = DslUtility.GetBaseChangesOnDependency(Subtype, existingConcepts);
                if (dependsOn.Count() == 0)
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

            var filterBySubtypePrototype = new FilterByInfo { Source = conceptInfo.Supertype, Parameter = "Rhetos.Dom.DefaultConcepts.FilterSubtype" };
            newConcepts.Add(new SubtypeExtendFilterInfo
            {
                IsSubtypeOf = conceptInfo,
                FilterBySubtype = filterBySubtypePrototype
            });

            // Add metadata for supertype computation (union):

            string hashId = conceptInfo.ImplementationName == "" ? "item.ID"
                : "DomUtility.GetSubtypeImplementationId(item.ID, " + DomUtility.GetSubtypeImplementationHash(conceptInfo.ImplementationName) + ")";

            foreach (DataStructureInfo dependsOn in DslUtility.GetBaseChangesOnDependency(conceptInfo.Subtype, existingConcepts))
                newConcepts.Add(new ChangesOnChangedItemsInfo
                {
                    Computation = conceptInfo.Supertype,
                    DependsOn = dependsOn,
                    FilterType = "Rhetos.Dom.DefaultConcepts.FilterSubtype",
                    FilterFormula = @"changedItems => new Rhetos.Dom.DefaultConcepts.FilterSubtype
                        {
                            Ids = changedItems.Select(item => " + hashId + @").ToArray(),
                            Subtype = " + CsUtility.QuotedString(conceptInfo.Subtype.Module.Name + "." + conceptInfo.Subtype.Name) + @",
                            ImplementationName = " + CsUtility.QuotedString(conceptInfo.ImplementationName) + @"
                        }"
                });

            // Add metadata for subtype implementation:

            PersistedSubtypeImplementationIdInfo subtypeImplementationColumn = null;
            if (conceptInfo.SupportsPersistedSubtypeImplementationColum())
            {
                subtypeImplementationColumn = new PersistedSubtypeImplementationIdInfo { Subtype = conceptInfo.Subtype, ImplementationName = conceptInfo.ImplementationName };
                newConcepts.Add(subtypeImplementationColumn);
            }

            // Automatic interface implementation:

            var implementationView = (SqlViewInfo)existingConcepts.FindByKey(conceptInfo.GetImplementationViewPrototype().GetKey());
            if (implementationView == null)
            {
                implementationView = new ExtensibleSubtypeSqlViewInfo { IsSubtypeOf = conceptInfo };
                newConcepts.Add(implementationView);

                if (subtypeImplementationColumn != null)
                    newConcepts.Add(new SqlDependsOnSqlObjectInfo
                    {
                        // The subtype implementation view will use the PersistedSubtypeImplementationColumn.
                        DependsOn = subtypeImplementationColumn.GetSqlObjectPrototype(),
                        Dependent = implementationView
                    });
            }

            // Redirect the developer-provided SQL dependencies from the "Is" concept to the implementation view:

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnDataStructureInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnDataStructureInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnIDInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnIDInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnModuleInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnModuleInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnPropertyInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnPropertyInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlFunctionInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnSqlFunctionInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlIndexInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnSqlIndexInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlObjectInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnSqlObjectInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlDependsOnSqlViewInfo>(dep => dep.Dependent, conceptInfo)
                .Select(dep => new SqlDependsOnSqlViewInfo { Dependent = implementationView, DependsOn = dep.DependsOn }));

            return newConcepts;
        }
    }
}
