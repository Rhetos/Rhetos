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
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    public class PersistenceStorage : IPersistenceStorage
    {
        private readonly IPersistenceStorageCommandBatch _persistenceCommandBatch;
        private readonly IPersistenceStorageObjectMappings _persistenceMappings;
        private readonly CommonConceptsRuntimeOptions _options;

        public PersistenceStorage(
            IPersistenceStorageCommandBatch persistenceCommandBatch,
            IPersistenceStorageObjectMappings persistenceMappings,
            CommonConceptsRuntimeOptions options)
        {
            _persistenceCommandBatch = persistenceCommandBatch;
            _persistenceMappings = persistenceMappings;
            _options = options;
        }

        public void Save<TEntity>(IEnumerable<TEntity> toInsert, IEnumerable<TEntity> toUpdate, IEnumerable<TEntity> toDelete) where TEntity : class, IEntity
        {
            if (toDelete != null)
                ExecuteInBatches(GetSortedList(toDelete, reverse: true), PersistenceStorageCommandType.Delete);

            if (toUpdate != null)
                ExecuteInBatches(new List<TEntity>(toUpdate), PersistenceStorageCommandType.Update);

            if (toInsert != null)
                ExecuteInBatches(GetSortedList(toInsert), PersistenceStorageCommandType.Insert);
        }

        private void ExecuteInBatches<TEntity>(List<TEntity> entities, PersistenceStorageCommandType commandType) where TEntity : class, IEntity
        {
            if (entities.Count == 0)
                return;

            var entityType = typeof(TEntity);
            for (int batchStart = 0; batchStart < entities.Count; batchStart += _options.SaveSqlCommandBatchSize)
            {
                var commands = entities.Skip(batchStart).Take(_options.SaveSqlCommandBatchSize)
                    .Select(e => new PersistenceStorageCommand { CommandType = commandType, Entity = e, EntityType = entityType })
                    .ToList();
                var numberOfAffectedRows = _persistenceCommandBatch.Execute(commands);
                CheckRowCount(numberOfAffectedRows, commands, commandType, entityType);
            }
        }

        private void CheckRowCount(int numberOfAffectedRows, List<PersistenceStorageCommand> commands, PersistenceStorageCommandType commandType, Type entityType)
        {
            if (numberOfAffectedRows < commands.Count && (commandType == PersistenceStorageCommandType.Delete || commandType == PersistenceStorageCommandType.Update))
            {
                string message = commandType == PersistenceStorageCommandType.Update
                    ? "Updating a record that does not exist in database."
                    : "Deleting a record that does not exist in database.";

                if (commands.Count == 1) // If there are multiple records, there is no information on which record is missing.
                    message += " ID=" + commands.Single().Entity.ID.ToString();

                throw new NonexistentRecordException(message);
            }
            else if (numberOfAffectedRows != commands.Count)
                throw new FrameworkException($"Unexpected number of rows affected on insert of '{entityType}'. Row count {numberOfAffectedRows}, expected {commands.Count}.");
        }

        private List<TEntity> GetSortedList<TEntity>(IEnumerable<TEntity> entities, bool reverse = false) where TEntity : IEntity
        {
            var entitiesCopy = new List<TEntity>(entities);
            if (entitiesCopy.Count == 0)
                return entitiesCopy;

            var mapper = _persistenceMappings.GetMapping(typeof(TEntity));
            var dependencies = entitiesCopy.SelectMany(x => mapper.GetDependencies(x).Select(y => new Tuple<Guid, Guid>(y, x.ID)));
            var ids = entitiesCopy.Select(x => x.ID).ToList();
            Graph.TopologicalSort(ids, dependencies);
            Graph.SortByGivenOrder(entitiesCopy, ids, plugin => plugin.ID);
            if (reverse)
                entitiesCopy.Reverse();
            return entitiesCopy;
        }
    }
}
