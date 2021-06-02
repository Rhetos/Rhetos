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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Restricts access for selected users to a subset of the entity's records.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissions")]
    public class RowPermissionsInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        public string SimplifiedExpression { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.Add(new RowPermissionsReadInfo() { SimplifiedExpression = SimplifiedExpression, Source = Source });
            newConcepts.Add(new RowPermissionsWriteInfo() { SimplifiedExpression = SimplifiedExpression, Source = Source });

            return newConcepts;
        }

        public static string CreateComposableFilterSnippet(string permissionExpressionName, DataStructureInfo dataStructure)
        {
            return $@"(source, parameter) => 
        {{
            var filterExpression = {permissionExpressionName}(source, _domRepository, _executionContext);
            return FilterExpression<Common.Queryable.{dataStructure.Module.Name}_{dataStructure.Name}>.OptimizedWhere(source, filterExpression);
        }}";
        }
    }
}
