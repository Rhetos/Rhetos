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

using Autofac;
using Autofac.Core;

namespace Rhetos.Extensibility
{
    public class PluginMetadataRegistrationSource : ImplicitRegistrationSource
    {
        public PluginMetadataRegistrationSource()
            : base(typeof(PluginMetadata<>))
        {
        }

        public override string Description => "PluginMetadata<T> Support";

        protected override object ResolveInstance<T>(IComponentContext ctx, ResolveRequest request)
            => new PluginMetadata<T>(request.Registration.Activator.LimitType, request.Registration.Target.Metadata);
    }
}
