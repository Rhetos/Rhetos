using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IPersistanceStorageCommandBatch
    {
        IPersistanceStorageCommandBatch Add<T>(T entity, PersistanceStorageCommandType commandType) where T : IEntity;

        int Execute();
    }

    public static class IPersistanceCommandBatchExtensions
    {
        public static IPersistanceStorageCommandBatch Add<T>(this IPersistanceStorageCommandBatch commandBatch, IEnumerable<T> entites, PersistanceStorageCommandType commandType) where T : IEntity
        {
            foreach (var entity in entites)
                commandBatch.Add(entity, commandType);
            return commandBatch;
        }
    }
}
