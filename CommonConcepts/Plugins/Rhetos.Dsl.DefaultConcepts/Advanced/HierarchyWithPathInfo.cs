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
    [ConceptKeyword("Hierarchy")]
    public class HierarchyWithPathInfo : HierarchyInfo, IMacroConcept
    {
        public string PathName { get; set; }
        public string GeneratePathFrom { get; set; }
        public string PathSeparator { get; set; }

        public new IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var pathProperty = new LongStringPropertyInfo
                {
                    DataStructure = GetComputedDataStructure(),
                    Name = PathName
                };

            return new IConceptInfo[] { pathProperty }
                .Concat(base.CreateNewConcepts(existingConcepts))
                .ToList();
        }

        protected override string ComputedDataStructureExpression()
        {
            return string.Format(@"repository =>
            {{
                try
                {{
                    var hierarchyItems = repository.{0}.{1}.Query().Select(item =>
                        new Rhetos.Dom.DefaultConcepts.HierarchyItem
                            {{
                                ID = item.ID,
                                ParentID = item.{2}.ID,
                                Name = item.{4}.ToString()
                            }}).ToArray();

                    {6}

                    var hierarchyIndexes = Rhetos.Dom.DefaultConcepts.HierarchyInfo.Compute(hierarchyItems, {5});
                    return hierarchyIndexes.Select(hi => new {0}.Compute{1}{2}Hierarchy
                    {{
                        ID = hi.ID,
                        LeftIndex = hi.LeftIndex,
                        RightIndex = hi.RightIndex,
                        Level = hi.Level,
                        {3} = hi.Path
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
               PathName,
               GeneratePathFrom,
               CsUtility.QuotedString(PathSeparator),
               BeforeRecomputeTag.Evaluate(this));
        }
    }
}
