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

        //===========================================================
        // Creating a view for the subtype's implementation of the supertype interface:

        public SqlViewInfo Dependency_ImplementationView { get; set; }

        public static readonly SqlTag<IsSubtypeOfInfo> PropertyImplementationTag = new SqlTag<IsSubtypeOfInfo>("PropertyImplementation");

        IEnumerable<string> IAlternativeInitializationConcept.DeclareNonparsableProperties()
        {
            return new[] { "Dependency_ImplementationView" };
        }

        void IAlternativeInitializationConcept.InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_ImplementationView = new SqlViewInfo
            {
                Module = Subtype.Module,
                Name = ImplementationViewName(),
                ViewSource = ImplementationViewSnippet()
            };

            createdConcepts = new[] { Dependency_ImplementationView };
        }

        public string GetSubtypeName()
        {
            return Subtype.Module.Name + "." + Subtype.Name;
        }

        public string GetSubtypeReferenceName()
        {
            return DslUtility.NameOptionalModule(Subtype, Supertype.Module);
        }

        private string ImplementationViewSnippet()
        {
            return string.Format(
@"SELECT
    ID{2}
FROM
    {0}.{1}",
                Subtype.Module.Name,
                Subtype.Name,
                PropertyImplementationTag.Evaluate(this));
        }

        private string ImplementationViewName()
        {
            return Subtype.Name + "_As_"
                + DslUtility.NameOptionalModule(Supertype, Subtype.Module);
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

            return newConcepts;
        }
    }
}
