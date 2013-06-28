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

namespace Rhetos.Factory
{
    public static class ITypeFactoryBuilderUtility
    {
        public static void RegisterType(this ITypeFactoryBuilder tfb, Type type)
        {
            tfb.RegisterBuilderType(new TypeFactoryBuilderType { Type = type, AsGeneric = false, Singleton = false });
        }

        public static void RegisterTypes(this ITypeFactoryBuilder tfb, IEnumerable<Type> types)
        {
            if (types != null)
                foreach (var t in types)
                    tfb.RegisterBuilderType(new TypeFactoryBuilderType { Type = t, AsGeneric = false, Singleton = false });
        }

        public static void RegisterInstance<T>(this ITypeFactoryBuilder tfb, T instance)
        {
            tfb.RegisterBuilderInstance(new TypeFactoryBuilderInstance { Instance = instance, AsType = typeof(T) });
        }

        public static void RegisterFunc<T>(this ITypeFactoryBuilder tfb, Func<ITypeFactory, T> func)
        {
            tfb.RegisterBuilderFunc(new TypeFactoryBuilderFunc { Func = tf => func(tf), AsType = typeof(T) });
        }

        public static void RegisterModule(this ITypeFactoryBuilder tfb, Autofac.Module autofacModule)
        {
            tfb.RegisterBuilderModule(autofacModule);
        }
    }

    internal class TypeFactoryBuilderHelper : ITypeFactoryBuilder
    {
        internal List<TypeFactoryBuilderType> Types = new List<TypeFactoryBuilderType>();
        internal List<TypeFactoryBuilderInstance> Instances = new List<TypeFactoryBuilderInstance>();
        internal List<TypeFactoryBuilderFunc> Funcs = new List<TypeFactoryBuilderFunc>();
        internal List<Autofac.Module> Modules = new List<Autofac.Module>();

        internal TypeFactoryBuilderHelper() { }

        internal TypeFactoryBuilderHelper(IEnumerable<Type> basicTypes)
        {
            Types.AddRange((from t in basicTypes select new TypeFactoryBuilderType { Type = t, AsGeneric = false, Singleton = false }));
        }

        public void RegisterBuilderType(TypeFactoryBuilderType type) { Types.Add(type); }
        public void RegisterBuilderInstance(TypeFactoryBuilderInstance instance) { Instances.Add(instance); }
        public void RegisterBuilderFunc(TypeFactoryBuilderFunc func) { Funcs.Add(func); }
        public void RegisterBuilderModule(Module autofacModule) { Modules.Add(autofacModule); }

        public IEnumerable<TypeFactoryBuilderType> GetRegisteredTypes() { return Types; }
        public IEnumerable<TypeFactoryBuilderInstance> GetRegisteredInstances() { return Instances; }
        public IEnumerable<TypeFactoryBuilderFunc> GetRegisteredFuncs() { return Funcs; }
        public IEnumerable<Module> GetRegisteredModules() { return Modules; }
    }
}
