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

using Rhetos.Dsl;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.DatabaseGenerator.Test
{
    public static class DatabaseObjectExtensions
    {
        public static ConceptApplication Find<T>(this IEnumerable<ConceptApplication> conceptApplications) where T : IConceptInfo
        {
            return conceptApplications.Where(ca => ca.ConceptInfoTypeName.StartsWith(typeof(T).FullName + ",")).Single();
        }

        public static DatabaseObject Find<T>(this IEnumerable<DatabaseObject> conceptApplications) where T : IConceptInfo
        {
            return conceptApplications.Where(ca => ca.ConceptInfoTypeName.StartsWith(typeof(T).FullName + ",")).Single();
        }
    }
}
