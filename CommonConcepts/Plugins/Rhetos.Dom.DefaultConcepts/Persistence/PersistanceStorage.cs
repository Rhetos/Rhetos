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

        public Action<int, DbCommand> AfterCommandExecution { get; set; }

        public PersistanceStorage(IPersistenceTransaction persistenceTransaction, IPersistanceStorageObjectMappings persistanceStorageMapping)
        {
            _persistenceTransaction = persistenceTransaction;
            _persistanceStorageMapping = persistanceStorageMapping;
        }

        public IPersistanceStorageCommandBatch StartBatch()
        {
            return new SqlCommandBatch(_persistenceTransaction, _persistanceStorageMapping, 20, AfterCommandExecution);
        }

        public int Insert<TEntity>(IEnumerable<TEntity> toInsert) where TEntity : IEntity
        {
            return StartBatch().Add(GetSorted(toInsert).Reverse(), PersistanceStorageCommandType.Insert).Execute();
        }

        public int Update<TEntity>(IEnumerable<TEntity> toUpdate) where TEntity : IEntity
        {
            return StartBatch().Add(GetSorted(toUpdate).Reverse(), PersistanceStorageCommandType.Update).Execute();
        }

        public int Delete<TEntity>(IEnumerable<TEntity> toDelete) where TEntity : IEntity
        {
            return StartBatch().Add(GetSorted(toDelete), PersistanceStorageCommandType.Delete).Execute();
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
