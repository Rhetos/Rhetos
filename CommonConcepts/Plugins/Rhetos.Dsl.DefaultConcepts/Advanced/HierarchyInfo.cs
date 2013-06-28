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
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Hierarchy")]
    public class HierarchyInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        public string Name { get; set; }

        public class HierarchyTag : Tag<HierarchyInfo>
        {
            public HierarchyTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        public static readonly HierarchyTag BeforeRecomputeTag = new HierarchyTag(TagType.Appendable, "/*Hierarchy.BeforeRecompute {0}.{1}.{2}*/");

        public ComputedInfo GetComputedDataStructure()
        {
            return new ComputedInfo
            {
                Module = DataStructure.Module,
                Name = "Compute" + DataStructure.Name + Name + "Hierarchy",
                Expression = ComputedDataStructureExpression()
            };
        }

        protected PersistedDataStructureInfo GetPersistedDataStructure()
        {
            return new PersistedDataStructureInfo
            {
                Module = DataStructure.Module,
                Name = DataStructure.Name + Name + "Hierarchy",
                Source = GetComputedDataStructure()
            };
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            ComputedInfo computedDataStructure = GetComputedDataStructure();
            PersistedDataStructureInfo persistedDataStructure = GetPersistedDataStructure();
            var persistedLeftIndexProperty = new IntegerPropertyInfo { DataStructure = persistedDataStructure, Name = "LeftIndex" };

            var dependencies = GetDependsOnWriteableDataStructure(DataStructure, existingConcepts, this);
            var computedDataStructureDependencies = dependencies.Select(dependsOn =>
                new ChangesOnChangedItemsInfo
                {
                    Computation = computedDataStructure,
                    DependsOn = dependsOn,
                    FilterType = "FilterAll",
                    FilterFormula = "changedItems => new FilterAll()"
                });

            var filterAncestorsParameter = new ParameterInfo { Module = DataStructure.Module, Name = Name + "HierarchyAncestors" };
            var filterDescendantsParameter = new ParameterInfo { Module = DataStructure.Module, Name = Name + "HierarchyDescendants" };

            return new IConceptInfo[]
            {
                new ReferencePropertyInfo { DataStructure = DataStructure, Name = Name, Referenced = DataStructure },

                // Computing the hierarcy information:
                computedDataStructure,
                new DataStructureExtendsInfo { Extension = computedDataStructure, Base = DataStructure },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "LeftIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "RightIndex" },
                new IntegerPropertyInfo { DataStructure = computedDataStructure, Name = "Level" },

                // Persisting the hierarcy information:
                persistedDataStructure,
                new PersistedAllPropertiesInfo { Persisted = persistedDataStructure }, // This will copy all properties from computedDataStructure.
                new KeepSynchronizedInfo { Persisted = persistedDataStructure },
                persistedLeftIndexProperty,
                new SqlIndexInfo { Property = persistedLeftIndexProperty },

                // Implement filters for finding ancestors and descendants, using indexed pesisted data:
                filterAncestorsParameter,
                filterDescendantsParameter,
                new GuidPropertyInfo { DataStructure = filterAncestorsParameter, Name = "ID" },
                new GuidPropertyInfo { DataStructure = filterDescendantsParameter, Name = "ID" },
                new ComposableFilterByInfo { Source = DataStructure, Parameter = Name + "HierarchyAncestors", Expression = FilterAncestorsExpression() },
                new ComposableFilterByInfo { Source = DataStructure, Parameter = Name + "HierarchyDescendants", Expression = FilterDescendantsExpression() },

            }.Concat(computedDataStructureDependencies);
        }

        /// <summary>
        /// Returns all entites that a given data structure is constructed from.
        /// If the given data structure depends is an entity, it will be the only item in the result.
        /// </summary>
        public static List<DataStructureInfo> GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, IEnumerable<IConceptInfo> allConcepts, IConceptInfo errorContext)
        {
            var dependencies = new List<DataStructureInfo>();
            GetDependsOnWriteableDataStructure(dataStructure, dependencies, allConcepts, errorContext, new HashSet<string>());
            return dependencies;
        }

        private static void GetDependsOnWriteableDataStructure(DataStructureInfo dataStructure, List<DataStructureInfo> dependencies, IEnumerable<IConceptInfo> allConcepts, IConceptInfo errorContext, HashSet<string> done)
        {
            var conceptKey = dataStructure.GetKey();
            if (done.Contains(conceptKey))
                return;
            done.Add(conceptKey);

            if (dataStructure is EntityInfo)
                dependencies.Add(dataStructure);
            else if (dataStructure is SqlQueryableInfo)
            {
                var deps = allConcepts.OfType<SqlDependsOnDataStructureInfo>().Where(dep => dep.Dependent == dataStructure).ToArray();
                foreach (var dep in deps)
                    GetDependsOnWriteableDataStructure(dep.DependsOn, dependencies, allConcepts, errorContext, done);
            }
            else
                throw new DslSyntaxException(errorContext.GetKeywordOrTypeName()
                    + " is not supported on dependency type '" + dataStructure.GetKeywordOrTypeName() + "'. "
                    + errorContext.GetUserDescription() + " depends on " + dataStructure.GetUserDescription() + ".");
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
                    var hierarchyIndexes = Rhetos.Dom.DefaultConcepts.HierarchyInfo.Compute(hierarchyItems, null);
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
                    throw new Rhetos.UserException(""It is not allowed to enter a circular dependency between records in hierarchy {0}.{1} by {2}."");
                }}
            }}",
               DataStructure.Module.Name,
               DataStructure.Name,
               Name,
               BeforeRecomputeTag.Evaluate(this));
        }

        protected virtual string FilterAncestorsExpression()
        {
            return string.Format(@"(items, repository, filterParameter) =>
            {{
                var child = repository.{0}.{1}.Query().Where(item => item.ID == filterParameter.ID).SingleOrDefault();
                if (child == null)
                    throw new Rhetos.UserException(""Given record does not exist: {0}.{1}, ID "" + filterParameter.ID + ""."");
                int leftIndex = child.Extension_{1}{2}Hierarchy.LeftIndex.Value;

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex < leftIndex
                    && item.Extension_{1}{2}Hierarchy.RightIndex > leftIndex);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }

        protected virtual string FilterDescendantsExpression()
        {
            return string.Format(@"(items, repository, filterParameter) =>
            {{
                var parent = repository.{0}.{1}.Query().Where(item => item.ID == filterParameter.ID).SingleOrDefault();
                if (parent == null)
                    throw new Rhetos.UserException(""Given record does not exist: {0}.{1}, ID "" + filterParameter.ID + ""."");
                int leftIndex = parent.Extension_{1}{2}Hierarchy.LeftIndex.Value;
                int rightIndex = parent.Extension_{1}{2}Hierarchy.RightIndex.Value;

                return items.Where(item =>
                    item.Extension_{1}{2}Hierarchy.LeftIndex > leftIndex
                    && item.Extension_{1}{2}Hierarchy.LeftIndex < rightIndex);
            }}",
                DataStructure.Module.Name,
                DataStructure.Name,
                Name);
        }
    }
}
