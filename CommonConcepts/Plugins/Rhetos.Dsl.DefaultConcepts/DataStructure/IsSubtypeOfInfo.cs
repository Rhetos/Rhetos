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
    public class IsSubtypeOfInfo : IMacroConcept, IAlternativeInitializationConcept
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

        //===========================================================
        // Creating a view for the subtype's implementation of the supertype interface:

        public SqlViewInfo Dependency_ImplementationView { get; set; }

        public static readonly SqlTag<IsSubtypeOfInfo> PropertyImplementationTag = new SqlTag<IsSubtypeOfInfo>("PropertyImplementation");

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_ImplementationView" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_ImplementationView = new SqlViewInfo
            {
                Module = Subtype.Module,
                Name = GetImplementationViewName(),
                ViewSource = ImplementationViewSnippet()
            };

            createdConcepts = new[] { Dependency_ImplementationView };
        }

        public string GetSubtypeReferenceName()
        {
            return DslUtility.NameOptionalModule(Subtype, Supertype.Module) + ImplementationName;
        }

        private string GetImplementationViewName()
        {
            string viewName = Subtype.Name + "_As_" + DslUtility.NameOptionalModule(Supertype, Subtype.Module);
            if (ImplementationName != "")
                viewName += "_" + ImplementationName;
            return viewName;
        }

        private string ImplementationViewSnippet()
        {
            return string.Format(
@"SELECT
    ID{3}{2}
FROM
    {0}.{1}",
                Subtype.Module.Name,
                Subtype.Name,
                PropertyImplementationTag.Evaluate(this),
                GetSpecificImplementationId());
        }

        public bool SupportsPersistedSubtypeImplementationColum()
        {
            return !string.IsNullOrEmpty(ImplementationName) && Subtype is EntityInfo;
        }

        /// <summary>
        /// Same subtype may implement same supertype multiple time. Since ID of the supertype is usually same as subtype's ID,
        /// that might result with multiple supertype records with the same ID. To avoid duplicate IDs and still keep the
        /// deterministic ID values, the supertype's ID is XORed by a hash code taken from the ImplementationName.
        /// </summary>
        private string GetSpecificImplementationId()
        {
            if (ImplementationName == "")
                return "";
            else if (SupportsPersistedSubtypeImplementationColum())
                return ",\r\n    SubtypeImplementationID = " + new SubtypeImplementationColumnInfo { Subtype = Subtype, ImplementationName = ImplementationName }.GetComputedColumnName();
            else
            {
                int hash = DomUtility.GetSubtypeImplementationHash(ImplementationName);
                return ",\r\n    SubtypeImplementationID = CONVERT(UNIQUEIDENTIFIER, CONVERT(BINARY(4), CONVERT(INT, CONVERT(BINARY(4), ID)) ^ " + hash + ") + SUBSTRING(CONVERT(BINARY(16), ID), 5, 12))";
            }
        }

        //===========================================================

        IEnumerable<IConceptInfo> IMacroConcept.CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Add a subtype reference (for each subtype) to the supertype data structure

            var subtypeReference = new ReferencePropertyInfo
            {
                DataStructure = Supertype,
                Referenced = Subtype,
                Name = GetSubtypeReferenceName()
            };
            newConcepts.Add(subtypeReference);
            newConcepts.Add(new PolymorphicPropertyInfo { Property = subtypeReference, SubtypeReference = Subtype.GetKeyProperties() });

            // Automatically add missing property implementations and missing properties to the subtype (automatic interface implementation)

            var implementableSupertypeProperties = existingConcepts.OfType<PolymorphicPropertyInfo>()
                .Where(pp => pp.Property.DataStructure == Supertype && pp.IsImplementable())
                .Select(pp => pp.Property).ToList();
            var subtypeProperties = existingConcepts.OfType<PropertyInfo>().Where(p => p.DataStructure == Subtype).ToList();
            var subtypeImplementsProperties = existingConcepts.OfType<SubtypeImplementsPropertyInfo>().Where(subim => subim.IsSubtypeOf == this).Select(subim => subim.Property).ToList();

            var missingImplementations = implementableSupertypeProperties.Except(subtypeImplementsProperties)
                .Select(supp => new SubtypeImplementsPropertyInfo
                {
                    IsSubtypeOf = this,
                    Property = supp,
                    Expression = supp.Name
                })
                .ToList();

            var missingProperties = missingImplementations.Select(subim => subim.Property).Where(supp => !subtypeProperties.Any(subp => subp.Name == supp.Name))
                .Select(supp => DslUtility.CreatePassiveClone(supp, Subtype))
                .ToList();

            newConcepts.AddRange(missingImplementations);
            newConcepts.AddRange(missingProperties);

            newConcepts.Add(new SqlDependsOnDataStructureInfo { Dependent = Dependency_ImplementationView, DependsOn = Subtype });

            string materializedUpdateSelector;
            if (ImplementationName == "")
                materializedUpdateSelector = "changedItems => changedItems.Select(item => item.ID).ToArray()";
            else
                materializedUpdateSelector = string.Format(
                    @"changedItems => changedItems.Select(item => DomUtility.GetSubtypeImplementationId(item.ID, {0})).ToArray()",
                    DomUtility.GetSubtypeImplementationHash(ImplementationName));

            newConcepts.Add(new ChangesOnChangedItemsInfo
            {
                Computation = Supertype,
                DependsOn = Subtype,
                FilterType = "System.Guid[]",
                FilterFormula = materializedUpdateSelector
            });

            if (SupportsPersistedSubtypeImplementationColum())
            {
                var subtypeImplementationColumn = new SubtypeImplementationColumnInfo { Subtype = Subtype, ImplementationName = ImplementationName };
                newConcepts.Add(subtypeImplementationColumn);
                newConcepts.Add(new SqlDependsOnSqlObjectInfo { Dependent = Dependency_ImplementationView, DependsOn = subtypeImplementationColumn.GetSqlObject() });
            }

            return newConcepts;
        }
    }
}
