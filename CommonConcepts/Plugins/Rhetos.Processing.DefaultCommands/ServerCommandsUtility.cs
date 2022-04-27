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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;

namespace Rhetos.Processing.DefaultCommands
{
    public class ServerCommandsUtility
    {
        private readonly ILogProvider _logProvider;
        private readonly GenericRepositories _repositories;

        public ServerCommandsUtility(ILogProvider logProvider, GenericRepositories repositories)
        {
            _logProvider = logProvider;
            _repositories = repositories;
        }

        /// <summary>
        /// Returns <see cref="EntityCommandsUtility"/> for the specified data structure name.
        /// </summary>
        /// <remarks>
        /// "Entity" in this context represents any data structure that implements IEntity with ID property,
        /// including Browse, SqlQueryable and other data structures.
        /// </remarks>
        public EntityCommandsUtility ForEntity(string entityName)
        {
            var entityRepository = _repositories.GetGenericRepository(entityName);
            return new EntityCommandsUtility(_logProvider, entityRepository);
        }
    }
}
