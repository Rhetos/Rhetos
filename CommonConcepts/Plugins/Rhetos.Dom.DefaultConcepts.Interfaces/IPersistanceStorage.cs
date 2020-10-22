using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IPersistanceStorage
    {
        Action<int, DbCommand> AfterCommandExecution { get; set; }

        IPersistanceStorageCommandBatch StartBatch();

        int Insert<TEntity>(IEnumerable<TEntity> toInsert) where TEntity : IEntity;

        int Update<TEntity>(IEnumerable<TEntity> toUpdate) where TEntity : IEntity;

        int Delete<TEntity>(IEnumerable<TEntity> toDelete) where TEntity : IEntity;
    }

    public static class IPersistanceStorageExtensions
    {
        public static int Insert<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toInsert) where TEntity : IEntity
        {
            return persistanceStorage.Insert(toInsert.Yield());
        }

        public static int Update<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toUpdate) where TEntity : IEntity
        {
            return persistanceStorage.Update(toUpdate.Yield());
        }

        public static int Delete<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toDelete) where TEntity : IEntity
        {
            return persistanceStorage.Delete(toDelete.Yield());
        }

        private static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
