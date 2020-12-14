using System;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IPersistanceStorageObjectMappings
    {
        IPersistanceStorageObjectMapper GetMapping(Type type);
    }
}
