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

using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    public class PersistanceStorage : IPersistanceStorage
    {
        private readonly IPersistenceTransaction _persistenceTransaction;

        private readonly IPersistanceStorageObjectMappings _persistanceStorageMapping;

        private readonly CommonConceptsRuntimeOptions _options;

        public Action<int, DbCommand> AfterCommandExecution { get; set; }

        public PersistanceStorage(IPersistenceTransaction persistenceTransaction, IPersistanceStorageObjectMappings persistanceStorageMapping, CommonConceptsRuntimeOptions options)
        {
            _persistenceTransaction = persistenceTransaction;
            _persistanceStorageMapping = persistanceStorageMapping;
            _options = options;
        }

        public IPersistanceStorageCommandBatch StartBatch()
        {
            return new SqlCommandBatch(_persistenceTransaction, _persistanceStorageMapping, _options.SaveSqlCommandBatchSize, AfterCommandExecution);
        }

        public void Insert<TEntity>(IEnumerable<TEntity> toInsert) where TEntity : class, IEntity
        {
            CsUtility.Materialize(ref toInsert);
            var numberOfAffectedRows = StartBatch().Add(GetSorted(toInsert).Reverse(), PersistanceStorageCommandType.Insert).Execute();
            CheckRowCount(numberOfAffectedRows, toInsert, PersistanceStorageCommandType.Insert, typeof(TEntity));

        }

        public void Update<TEntity>(IEnumerable<TEntity> toUpdate) where TEntity : class, IEntity
        {
            CsUtility.Materialize(ref toUpdate);
            var numberOfAffectedRows = StartBatch().Add(GetSorted(toUpdate).Reverse(), PersistanceStorageCommandType.Update).Execute();
            CheckRowCount(numberOfAffectedRows, toUpdate, PersistanceStorageCommandType.Update, typeof(TEntity));
        }

        public void Delete<TEntity>(IEnumerable<TEntity> toDelete) where TEntity : class, IEntity
        {
            CsUtility.Materialize(ref toDelete);
            var numberOfAffectedRows = StartBatch().Add(GetSorted(toDelete), PersistanceStorageCommandType.Delete).Execute();
            CheckRowCount(numberOfAffectedRows, toDelete, PersistanceStorageCommandType.Delete, typeof(TEntity));
        }

        private void CheckRowCount(int numberOfAffectedRows, IEnumerable<IEntity> saveItems, PersistanceStorageCommandType commandType, Type entityType)
        {
            if (numberOfAffectedRows < saveItems.Count() && (commandType == PersistanceStorageCommandType.Delete || commandType == PersistanceStorageCommandType.Update))
            {
                string message = commandType == PersistanceStorageCommandType.Update
                    ? "Updating a record that does not exist in database."
                    : "Deleting a record that does not exist in database.";

                if (saveItems.Count() == 1) // If there are multiple records, there is no information on which record is missing.
                    message += " ID=" + saveItems.Single().ID.ToString();

                throw new NonexistentRecordException(message);
            }
            else if (numberOfAffectedRows != saveItems.Count())
                throw new FrameworkException($"Unexpected number of rows affected on insert of '{entityType}'. Row count {numberOfAffectedRows}, expected {saveItems.Count()}.");
        }

        private IEnumerable<TEntity> GetSorted<TEntity>(IEnumerable<TEntity> entites) where TEntity : IEntity
        {
            var entitesCopy = new List<TEntity>(entites);
            var mapper = _persistanceStorageMapping.GetMapping(typeof(TEntity));
            var dependencies = entitesCopy.SelectMany(x => mapper.GetDependencies(x).Select(y => new Tuple<Guid, Guid>(x.ID, y)));
            var ids = entitesCopy.Select(x => x.ID).ToList();
            Graph.TopologicalSort(ids, dependencies);
            Graph.SortByGivenOrder(entitesCopy, ids, plugin => plugin.ID);
            return entitesCopy;
        }
    }
}
