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

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// This concept implementation is used for concepts that have no database implementation.
    /// This is useful for handling dependencies between database objects in situations where one database objects depends on another concept info
    /// that has no implementation and which depends on a third database object. First database object should indirectly depend on third, even though there
    /// is no second database object implementation.  Such scenarios are easier to handle if every concept has its implementation.
    /// </summary>
    public class NullImplementation : IConceptDatabaseDefinition
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
    }
}