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

using Rhetos.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Adds a self-reference property (for example, a superior in the employee hierarchy).
    /// *  Generates a cached index for optimized recursive queries(for example, find all direct and indirect subordinates) in the extension &lt;EntityName&gt;&lt;HierarchyName&gt;Hierarchy.
    /// *  Generates ComposableFilters &lt;HierarchyName&gt;HierarchyDescendants and &lt;HierarchyName&gt;HierarchyAncestors for quick access of all direct and indirect child records or parents.
    /// *  Generates validations to deny entering data with circular dependencies.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Hierarchy")]
    public class HierarchyInfo : ReferencePropertyInfo, IAlternativeInitializationConcept
    {
        public static readonly CsTag<HierarchyInfo> BeforeRecomputeTag = "BeforeRecompute";

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Referenced" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            if (Referenced != null && Referenced != DataStructure)
                throw new DslConceptSyntaxException(this, string.Format(
                    "Incorrectly constructed Hierarchy property: it should reference itself. Reference='{0}', DataStructure='{1}'.",
                    Referenced.GetUserDescription(),
                    DataStructure.GetUserDescription()));

            Referenced = DataStructure;
            createdConcepts = null;
        }

        public ComputedInfo GetComputedDataStructure()
        {
            return new ComputedInfo
            {
                Module = DataStructure.Module,
                Name = "Compute" + DataStructure.Name + Name + "Hierarchy",
                Expression = ComputedDataStructureExpression()
            };
        }

        public EntityInfo GetPersistedDataStructure()
        {
            return new EntityInfo
            {
                Module = DataStructure.Module,
                Name = DataStructure.Name + Name + "Hierarchy",
            };
        }
        protected virtual string ComputedDataStructureExpression()
        {
            return string.Format(@"repository =>
            {{
                try
                {{
                    var hierarchyItems = repository.{0}.{1}.Query().Select(item =>
                        new Rhetos.Dom.DefaultConcepts.HierarchyItem
                            {{
                                ID = item.ID,
                                ParentID = item.{2}.ID
                            }}).ToArray();

                    {3}
                    var hierarchyIndexes = Rhetos.Dom.DefaultConcepts.HierarchyIndexes.Compute(hierarchyItems, null);
                    return hierarchyIndexes.Select(hi => new {0}.Compute{1}{2}Hierarchy
                    {{
                        ID = hi.ID,
                        LeftIndex = hi.LeftIndex,
                        RightIndex = hi.RightIndex,
                        Level = hi.Level
                    }}).ToArray();
                }}
                catch (Rhetos.Dom.DefaultConcepts.HierarchyCircularReferenceException)
                {{
                    throw new Rhetos.UserException(
                        ""It is not allowed to enter a circular dependency between records in hierarchy {{0}} by {{1}}."",
                        new[] {{ _localizer[""{0}.{1}""], _localizer[""{2}""] }}, null, null);
                }}
            }}",
               DataStructure.Module.Name,
               DataStructure.Name,
               Name,
               BeforeRecomputeTag.Evaluate(this));
        }

        public virtual string FilterAncestorsExpression()
        {
            return string.Format(@"(items, parameter) =>
            {{
                int? leftIndex = _domRepository.{0}.{1}.Query().Where(item => item.ID == parameter.ID)
                    .Select(child => child.Extension_{1}{2}Hierarchy.LeftIndex)
                    .SingleOrDefault();
                if (leftIndex == null)
                    throw new Rhetos.UserException(""Given record does not exist or isn't initialized correctly: {{0}}, ID {{1}}."", new object[] {{ _localizer[""{0}.{1}""], parameter.ID }}, null, null);

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex < leftIndex.Value
                    && item.Extension_{1}{2}Hierarchy.RightIndex > leftIndex.Value);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }

        public virtual string FilterDescendantsExpression()
        {
            return string.Format(@"(items, parameter) =>
            {{
                var parent = _domRepository.{0}.{1}.Query().Where(item => item.ID == parameter.ID)
                    .Select(p => new
                        {{
                            LeftIndex = p.Extension_{1}{2}Hierarchy.LeftIndex.Value,
                            RightIndex = p.Extension_{1}{2}Hierarchy.RightIndex.Value
                        }})
                    .SingleOrDefault();
                if (parent == null)
                    throw new Rhetos.UserException(""Given record does not exist: {{0}}, ID {{1}}."", new object[] {{ _localizer[""{0}.{1}""], parameter.ID }}, null, null);

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex > parent.LeftIndex
                    && item.Extension_{1}{2}Hierarchy.LeftIndex < parent.RightIndex);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class HierarchyMacro : IConceptMacro<HierarchyInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(HierarchyInfo conceptInfo, IDslModel existingConcepts)
        {
            ComputedInfo computedDataStructure = conceptInfo.GetComputedDataStructure();

            var persistedDataStructure = conceptInfo.GetPersistedDataStructure();
            var persistedComputedFrom = new EntityComputedFromInfo { Target = persistedDataStructure, Source = computedDataStructure };

            var persistedLeftIndexProperty = new IntegerPropertyInfo { DataStructure = persistedDataStructure, Name = "LeftIndex" };

            var dependencies = GetDependsOnWriteableDataStructure(conceptInfo.DataStructure, existingConcepts, conceptInfo);
            var computedDataStructureDependencies = dependencies.Select(dependsOn =>
                new ChangesOnChangedItemsInfo
                {
                    Computation = computedDataStructure,
                    DependsOn = dependsOn,
                    FilterType = "FilterAll",
                    FilterFormula = "changedItems => new FilterAll()"
                });

            var filterAncestorsParameter = new ParameterInfo { Module = conceptInfo.DataStructure.Module, Name = conceptInfo.Name + "HierarchyAncestors" };
            var filterDescendantsParameter = new ParameterInfo { Module = conceptInfo.DataStructure.Module, Name = conceptInfo.Name + "HierarchyDescendants" };

            return new IConceptInfo[]
            {
                // Computing the hierarchy information:
                computedDataStructure,
                new DataStructureLocalizerInfo { DataStructure = computedDataStructure }, // computedDataStructure code snippet uses '_localizer' property.
                new DataStructureExtendsInfo { Extension = computedDataStructure, Base = conceptInfo.DataStructure },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "LeftIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "RightIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "Level" },

                // Persisting the hierarchy information:
                persistedDataStructure,
                persistedComputedFrom,
                new RepositoryMemberInfo { DataStructure = persistedDataStructure, Name = "Recompute", Implementation = PersistedHelpers.RecomputeMethodImplementation(persistedComputedFrom) },
                new EntityComputedFromAllPropertiesInfo { EntityComputedFrom = persistedComputedFrom }, // This will copy all properties from computedDataStructure.
                new KeepSynchronizedInfo { EntityComputedFrom = persistedComputedFrom, FilterSaveExpression = "" },
                persistedLeftIndexProperty,
                new SqlIndexMultipleInfo { DataStructure = persistedLeftIndexProperty.DataStructure, PropertyNames = persistedLeftIndexProperty.Name },

                // Implement filters for finding ancestors and descendants, using indexed persisted data:
                filterAncestorsParameter,
                filterDescendantsParameter,
                new GuidPropertyInfo { DataStructure = filterAncestorsParameter, Name = "ID" },
                new GuidPropertyInfo { DataStructure = filterDescendantsParameter, Name = "ID" },
                new QueryFilterExpressionInfo { Source = conceptInfo.DataStructure, Parameter = conceptInfo.Name + "HierarchyAncestors", Expression = conceptInfo.FilterAncestorsExpression() },
                new QueryFilterExpressionInfo { Source = conceptInfo.DataStructure, Parameter = conceptInfo.Name + "HierarchyDescendants", Expression = conceptInfo.FilterDescendantsExpression() },
                new DataStructureLocalizerInfo { DataStructure = conceptInfo.DataStructure }, // FilterAncestorsExpression() and FilterDescendantsExpression() use '_localizer' property.

            }.Concat(computedDataStructureDependencies);
        }

        /// <summary>
        /// Returns all entities that a given data structure is constructed from.
        /// If the given data structure depends is an entity, it will be the only item in the result.
        /// </summary>
        public static List<DataStructureInfo> GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, IDslModel allConcepts, IConceptInfo errorContext)
        {
            var dependencies = new List<DataStructureInfo>();
            GetDependsOnWriteableDataStructure(dataStructure, dependencies, allConcepts, errorContext, new HashSet<string>());
            return dependencies;
        }

        private static void GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, List<DataStructureInfo> dependencies, IDslModel allConcepts, IConceptInfo errorContext, HashSet<string> done)
        {
            var conceptKey = dataStructure.GetKey();
            if (done.Contains(conceptKey))
                return;
            done.Add(conceptKey);

            if (dataStructure is EntityInfo)
                dependencies.Add(dataStructure);
            else if (dataStructure is SqlQueryableInfo)
            {
                var deps = allConcepts.FindByReference<SqlDependsOnDataStructureInfo>(dep => dep.Dependent, dataStructure).ToArray();
                foreach (var dep in deps)
                    GetDependsOnWriteableDataStructure(dep.DependsOn, dependencies, allConcepts, errorContext, done);
            }
            else
                throw new DslSyntaxException(errorContext.GetKeywordOrTypeName()
                    + " is not supported on dependency type '" + dataStructure.GetKeywordOrTypeName() + "'. "
                    + errorContext.GetUserDescription() + " depends on " + dataStructure.GetUserDescription() + ".");
        }
    }
}
