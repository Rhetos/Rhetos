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
