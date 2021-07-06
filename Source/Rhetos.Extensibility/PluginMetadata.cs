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

namespace Rhetos.Extensibility
{
    /// <summary>
    /// The <see cref="PluginMetadata{T}"/> is the same as <see cref="Autofac.Features.Metadata.Meta{T}"/>
    /// except that it does not resolve the underlying component.
    /// The downside of such metadata resolution is that it relies on the implementation type 
    /// that is specified during the registration.
    /// In some cases the implementation can't be inferred unless the component is resolved.
    /// That can happen when using delegate registrations and then casting to some type, which is not advised
    /// https://autofac.readthedocs.io/en/latest/best-practices/index.html#use-as-t-in-delegate-registrations
    /// </summary>
    public class PluginMetadata<T>
    {
        public PluginMetadata(Type pluginType, IDictionary<string, object> metadata)
        {
            PluginType = pluginType;
            Metadata = metadata;
        }

        public Type PluginType { get; }

        public IDictionary<string, object> Metadata { get; }
    }
}
