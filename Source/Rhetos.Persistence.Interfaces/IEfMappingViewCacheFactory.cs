using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Persistence
{
    public interface IEfMappingViewCacheFactory
    {
        void RegisterFactoryForContext(IPersistenceCache persistenceCache);
    }
}
