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
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Implements")]
    public class SubtypeImplementsPropertyInfo : IAlternativeInitializationConcept, IValidatedConcept
    {
        [ConceptKey]
        public IsSubtypeOfInfo IsSubtypeOf { get; set; }

        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Expression { get; set; }

        public SqlViewInfo Dependency_ImplementationView { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_ImplementationView" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_ImplementationView = IsSubtypeOf.GetImplementationViewPrototype();
            createdConcepts = null;
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property, IsSubtypeOf.Supertype, this);

            if (!(Dependency_ImplementationView is ExtensibleSubtypeSqlViewInfo))
                throw new DslSyntaxException(this, "This property implementation cannot be used together with '" + Dependency_ImplementationView.GetUserDescription()
                    + "'. Use either " + this.GetKeywordOrTypeName() + " or " + Dependency_ImplementationView.GetKeywordOrTypeName() + ".");
        }
    }

    [Export(typeof(IConceptMacro))]
    public class SubtypeImplementsPropertyMacro : IConceptMacro<SubtypeImplementsPropertyInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SubtypeImplementsPropertyInfo conceptInfo, IDslModel existingConcepts)
        {
            var allProperties = existingConcepts.FindByReference<PropertyInfo>(p => p.DataStructure, conceptInfo.IsSubtypeOf.Subtype)
                .ToDictionary(p => p is ReferencePropertyInfo ? p.Name + "ID" : p.Name);

            var usedColumns = SqlAnalysis.ExtractPossibleColumnNames(conceptInfo.Expression);
            var usedProperties = usedColumns.Select(c => allProperties.GetValueOrDefault(c)).Where(p => p != null);

            return usedProperties.Select(p => new SqlDependsOnPropertyInfo
                {
                    Dependent = conceptInfo.Dependency_ImplementationView,
                    DependsOn = p
                });
        }
    }
}
