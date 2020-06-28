using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.MappingViews;
using System.Text;

namespace Rhetos.Persistence
{
    public class EfMappingViewCache : DbMappingViewCache
    {
        public override string MappingHashValue => _mappingViews?.Hash;

        private readonly EfMappingViews _mappingViews;

        public EfMappingViewCache(EfMappingViews mappingViews)
        {
            _mappingViews = mappingViews;
        }

        public override DbMappingView GetView(EntitySetBase extent)
        {
            if (!_mappingViews.Views.TryGetValue(GetExtentKey(extent), out var cached))
                return null;

            return new DbMappingView(cached);
        }

        public static string GetExtentKey(EntitySetBase entitySet)
        {
            return $"{entitySet.EntityContainer.Name}.{entitySet.Name}";
        }

    }
}
