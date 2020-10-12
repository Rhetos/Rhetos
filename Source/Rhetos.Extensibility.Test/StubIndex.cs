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

using Autofac.Features.Indexed;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility.Test
{
    public class StubIndex<TPlugin> : IIndex<Type, IEnumerable<TPlugin>>
    {
        private readonly PluginsMetadataList<TPlugin> _pluginsWithMedata;

        public StubIndex(PluginsMetadataList<TPlugin> pluginsWithMedata)
        {
            _pluginsWithMedata = pluginsWithMedata;
        }
        public StubIndex()
        {
            _pluginsWithMedata = new PluginsMetadataList<TPlugin>();
        }
        public bool TryGetValue(Type key, out IEnumerable<TPlugin> value)
        {
            value = this[key];
            return true;
        }
        public IEnumerable<TPlugin> this[Type key]
        {
            get
            {
                return _pluginsWithMedata
                    .Where(pm => pm.Metadata.Any(metadata => metadata.Key == MefProvider.Implements && (Type)metadata.Value == key))
                    .Select(pm => pm.Plugin)
                    .ToArray();
            }
        }
    }
}
