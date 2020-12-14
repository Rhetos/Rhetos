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
