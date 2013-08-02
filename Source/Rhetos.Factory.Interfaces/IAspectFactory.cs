/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Linq;
using System.Text;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Factory
{
    [ContractClass(typeof(AspectFactoryContract))]
    public interface IAspectFactory
    {
        void RegisterAspect<TAspected>(IAspect aspect);
        //void RegisterAspect(Type type, AspectType aspectType, IAspect aspect, Type[] implementsTypes);

        object CreateProxy(Type type, object value);
        TProxy CreateProxy<TProxy>(object value);
    }

    [ContractClassFor(typeof(IAspectFactory))]
    sealed class AspectFactoryContract : IAspectFactory
    {
        public void RegisterAspect<TAspected>(IAspect aspect)
        {
            Contract.Requires(aspect != null);
        }

        /*public void RegisterAspect(Type type, AspectType aspectType, IAspect aspect, Type[] implementsTypes)
        {
            Contract.Requires(type != null);
            Contract.Requires(aspect != null);
        }*/

        public object CreateProxy(Type type, object value)
        {
            Contract.Requires(type != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<object>() != null);
            return null;
        }

        public TProxy CreateProxy<TProxy>(object value)
        {
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<TProxy>() != null);
            return default(TProxy);
        }
    }
}
