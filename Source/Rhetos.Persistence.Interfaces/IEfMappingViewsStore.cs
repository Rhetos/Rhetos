using System;

namespace Rhetos.Persistence
{
    public interface IEfMappingViewsStore
    {
        EfMappingViews Load();
        void Save(EfMappingViews views);
    }
}
