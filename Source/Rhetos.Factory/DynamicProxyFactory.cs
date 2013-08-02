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
using Castle.DynamicProxy;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Factory
{
    public class DynamicProxyFactory : IAspectFactory
    {
        private readonly Dictionary<Type, RegisteredAspects> RegisteredAspects = new Dictionary<Type, RegisteredAspects>();
        private readonly ProxyGenerator ProxyGenerator = new ProxyGenerator(new PersistentProxyBuilder());

        private readonly IComponentInterceptorRegistrator Interceptor;

        public DynamicProxyFactory(IComponentInterceptorRegistrator interceptor)
        {
            Contract.Requires(interceptor != null);

            this.Interceptor = interceptor;
        }

        public void RegisterAspect<TAspected>(IAspect aspect)
        {
            RegisterNewAspect(typeof(TAspected), /*AspectType.InterfaceWithTarget, */aspect/*, null*/);
        }
        /*
        public void RegisterAspect(Type type, AspectType aspectType, IAspect aspect, Type[] implementsTypes)
        {
            RegisterNewAspect(type, aspectType, aspect, implementsTypes);
        }
        */
        private void RegisterNewAspect(Type type, /*AspectType aspectType, */IAspect aspect/*, Type[] implementsTypes*/)
        {
            RegisteredAspects registeredAspect;
            if (!RegisteredAspects.TryGetValue(type, out registeredAspect))
            {
                registeredAspect = new RegisteredAspects(/*aspectType*/);
                RegisteredAspects.Add(type, registeredAspect);

                Interceptor.RegisterOnActivating(
                    type,
                    handler => handler.Instance = CreateProxyFromAspect(registeredAspect, type, handler.Instance));
            }
            /*else if (registeredAspect.AspectType != aspectType)
                throw new FrameworkException("Aspect of type " + aspectType + " has already been registered on " + type.FullName);
            */
            registeredAspect.RegisterNewAspect(aspect/*, implementsTypes*/);
        }

        public object CreateProxy(Type type, object value)
        {
            RegisteredAspects aspect;
            if (RegisteredAspects.TryGetValue(type, out aspect))
                return CreateProxyFromAspect(aspect, type, value);
            return value;
        }

        private object CreateProxyFromAspect(RegisteredAspects aspect, Type type, object value)
        {
            ProxyGenerationOptions options = new ProxyGenerationOptions { Selector = aspect.PointcutSelector };

            //if (aspect.AspectType == AspectType.InterfaceWithTarget)
            return ProxyGenerator.CreateInterfaceProxyWithTarget(type, aspect.AllTypes.ToArray(), value, options, aspect.AllAdvices.ToArray());
            /*if (aspect.AspectType == AspectType.InterfaceWithoutTarget)
                return ProxyGenerator.CreateInterfaceProxyWithoutTarget(type, aspect.AllTypes.ToArray(), options, aspect.AllAdvices.ToArray());
            if (aspect.AspectType == AspectType.Class)
                return ProxyGenerator.CreateClassProxy(type, aspect.AllTypes.ToArray(), options, aspect.AllAdvices.ToArray());
            
            throw new FrameworkException("Not implemented yet");*/
        }

        public TProxy CreateProxy<TProxy>(object value)
        {
            return (TProxy)CreateProxy(typeof(TProxy), value);
        }
    }
}
