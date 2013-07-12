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
using Autofac;
using Castle.DynamicProxy;
using Rhetos.Extensibility;
using Autofac.Core;
using Autofac.Builder;
using System.Diagnostics.Contracts;
using Rhetos.Compiler;

namespace Rhetos.Factory
{
    public class TypeFactory : ITypeFactory
    {
        private ILifetimeScope FactoryScope;
        private ILifetimeScope LastFactoryScope;
        private ILifetimeScope MyRootContainerScope;
        private readonly IAspectFactory AspectFactory;

        private readonly Dictionary<Type, Func<Type, object>> TypeFactoryInstancerCache = new Dictionary<Type, Func<Type, object>>();

        private readonly List<ITypeFactoryBuilder> RegisteredBuilders = new List<ITypeFactoryBuilder>();

        public TypeFactory(
            ILifetimeScope containerScope,
            IAspectFactory aspectFactory)
        {
            Contract.Requires(containerScope != null);
            Contract.Requires(aspectFactory != null);

            this.FactoryScope = containerScope;
            this.AspectFactory = aspectFactory;
            this.MyRootContainerScope = null;
            this.LastFactoryScope = containerScope;
        }

        public void Register(ITypeFactoryBuilder builder)
        {
            lock (RegisteredBuilders)
            {
                if (FactoryScope != null)
                {
                    LastFactoryScope = FactoryScope;
                    FactoryScope = null;
                }

                RegisteredBuilders.Add(builder);
            }
        }

        private void ProcessBuilder(ContainerBuilder containerBuilder, ITypeFactoryBuilder typeBuilder)
        {
            var types = typeBuilder.GetRegisteredTypes();
            if (types != null)
            {
                // TODO: Use RegistrationBuilder instead of the if/elseif.
				
                var baseTypes = (from t in types where !t.AsGeneric select t);
                foreach (var t in baseTypes)
                {
                    if (t.AsType != null)
                    {
                        if (t.Singleton)
                            containerBuilder.RegisterType(t.Type).As(t.AsType).SingleInstance();
                        else
                            containerBuilder.RegisterType(t.Type).As(t.AsType);
                    }
                    else
                    {
                        if (t.Singleton)
                            containerBuilder.RegisterType(t.Type).SingleInstance();
                        else
                            containerBuilder.RegisterType(t.Type);
                    }
                }
                var genericTypes = (from t in types where t.AsGeneric select t);
                foreach (var t in genericTypes)
                {
                    if (t.AsType != null)
                    {
                        if (t.Singleton)
                            containerBuilder.RegisterGeneric(t.Type).As(t.AsType).SingleInstance();
                        else
                            containerBuilder.RegisterGeneric(t.Type).As(t.AsType);
                    }
                    else
                    {
                        if (t.Singleton)
                            containerBuilder.RegisterGeneric(t.Type).SingleInstance();
                        else
                            containerBuilder.RegisterGeneric(t.Type);
                    }
                }
            }
            var instances = typeBuilder.GetRegisteredInstances();
            if (instances != null)
            {
                foreach (var i in instances)
                {
                    if (i.AsType != null)
                        containerBuilder.RegisterInstance(i.Instance).As(i.AsType);
                    else
                        containerBuilder.RegisterInstance(i.Instance).As(i.Instance.GetType());
                }
            }
            var funcs = typeBuilder.GetRegisteredFuncs();
            if (funcs != null)
            {
                foreach (var f in funcs)
                {
                    var localf = f;
                    if (localf.AsType != null)
                        containerBuilder.Register(c => localf.Func(c.Resolve<ITypeFactory>())).As(localf.AsType);
                    else
                        containerBuilder.Register(c => localf.Func(c.Resolve<ITypeFactory>()));
                }
            }
            var modules = typeBuilder.GetRegisteredModules();
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    containerBuilder.RegisterModule(module);
                }
            }
        }

        private void BuildFactory()
        {
            FactoryScope = LastFactoryScope.BeginLifetimeScope(
                cb =>
                {
                    foreach (var tb in RegisteredBuilders)
                        if (tb != null)
                            ProcessBuilder(cb, tb);
                });

            RegisteredBuilders.Clear();
            TypeFactoryInstancerCache.Clear();

            if (MyRootContainerScope == null)
                MyRootContainerScope = FactoryScope;
        }

        public bool IsRegistered(Type type)
        {
            lock (RegisteredBuilders)
            {
                if (FactoryScope == null)
                    BuildFactory();
                return FactoryScope.IsRegistered(type);
            }
        }


        public ITypeFactory CreateInnerTypeFactory()
        {
            lock (RegisteredBuilders)
            {
                if (FactoryScope == null)
                    BuildFactory();
                return new TypeFactory(FactoryScope, AspectFactory);
            }
        }

        public object CreateInstance(Type type)
        {
            lock (RegisteredBuilders)
            {
                if (FactoryScope == null)
                    BuildFactory();

                Func<Type, object> instancer;
                if (!TypeFactoryInstancerCache.TryGetValue(type, out instancer))
                {
                    bool inContainer = FactoryScope.IsRegistered(type);

                    if (!inContainer)
                        instancer = td => AspectFactory.CreateProxy(type, Activator.CreateInstance(td));
                    else
                        instancer = td => FactoryScope.Resolve(td);

                    TypeFactoryInstancerCache.Add(type, instancer);
                }

                return instancer(type);
            }
        }

        public T CreateInstanceKeyed<T>(object key)
        {
            lock (RegisteredBuilders)
            {
                if (FactoryScope == null)
                    BuildFactory();

                return FactoryScope.ResolveKeyed<T>(key);
            }
        }

        public void Dispose()
        {
            if (MyRootContainerScope != null)
                MyRootContainerScope.Dispose();
        }

    }
}
