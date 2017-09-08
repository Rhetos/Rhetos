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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A helper for defining a computation dependency to the referenced entity.
    /// * The ReferencePath can include the 'Base' reference from extended concepts.
    /// * The ReferencePath can target a Polymorphic. This will generate a ChangesOnChangesItems for each Polymorphic implementation.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ChangesOnReferenced")]
    public class ChangesOnReferencedInfo : IConceptInfo, IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo Computation { get; set; }

        [ConceptKey]
        public string ReferencePath { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.ValidatePath(Computation, ReferencePath, existingConcepts, this);
            var persistedEntities = existingConcepts.FindByReference<EntityComputedFromInfo>(cf => cf.Source, Computation)
                .Select(cf => cf.Target);
            foreach (var persisted in persistedEntities)
                DslUtility.ValidatePath(persisted, ReferencePath, existingConcepts, this);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class ChangesOnReferencedMacro : IConceptMacro<ChangesOnReferencedInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ChangesOnReferencedInfo conceptInfo, IDslModel existingConcepts)
        {
            var reference = DslUtility.GetPropertyByPath(conceptInfo.Computation, conceptInfo.ReferencePath, existingConcepts);
            if (reference.IsError)
                return null; // Wait for the other macro concepts to be evaluated. If the error persists, it will be handled at IValidatedConcept.CheckSemantics.
            if (!(reference.Value is ReferencePropertyInfo))
                throw new DslSyntaxException(conceptInfo, $"The given path '{conceptInfo.ReferencePath}' should end with a reference property, instead of the {reference.Value.GetUserDescription()}.");
            var referenced = ((ReferencePropertyInfo)reference.Value).Referenced;

            var computationDependencies = new List<Tuple<DataStructureInfo, string>>();
            if (referenced is IWritableOrmDataStructure)
                computationDependencies.Add(Tuple.Create(referenced, "item => item.ID"));
            else if (referenced is PolymorphicInfo)
                AddPolymorphicImplementations(computationDependencies, (PolymorphicInfo)referenced, existingConcepts, conceptInfo);

            string referencePathWithGuid = ChangeReferenceToGuid(conceptInfo.ReferencePath);

            return computationDependencies.Select(dep =>
                new ChangesOnChangedItemsInfo
                {
                    Computation = conceptInfo.Computation,
                    DependsOn = dep.Item1,
                    FilterType = "FilterCriteria",
                    FilterFormula = $@"changedItems => new FilterCriteria({CsUtility.QuotedString(referencePathWithGuid)}, ""In"","
                        + $" _domRepository.Common.FilterId.CreateQueryableFilterIds(changedItems.Select({dep.Item2})))"
                });
        }

        private static string ChangeReferenceToGuid(string referencePath)
        {
            if (referencePath == "ID" || referencePath.EndsWith(".ID"))
                return referencePath;
            else if (referencePath == "Base" || referencePath.EndsWith(".Base"))
                return referencePath.Substring(0, referencePath.Length - 4) + "ID";
            else
                return referencePath + "ID";
        }

        private void AddPolymorphicImplementations(List<Tuple<DataStructureInfo, string>> computationDependencies, PolymorphicInfo polymorphic, IDslModel existingConcepts, IConceptInfo errorContext)
        {
            var implementations = existingConcepts.FindByReference<IsSubtypeOfInfo>(imp => imp.Supertype, polymorphic);
            var unsupported = implementations.Where(imp => !(imp.Subtype is IWritableOrmDataStructure)).FirstOrDefault();
            if (unsupported != null)
                throw new DslSyntaxException(errorContext, $"The referenced '{polymorphic.GetUserDescription()}' is not supported"
                    + $" because it contains a non-writable implementation '{unsupported.Subtype.GetUserDescription()}'."
                    + $" Please use ChangesOnChangedItems instead, to manually set the computation's dependencies.");

            computationDependencies.AddRange(implementations.Select(imp => Tuple.Create(imp.Subtype,
                IsSubtypeOfMacro.GetComputeHashIdSelector(imp))));
        }
    }
}
