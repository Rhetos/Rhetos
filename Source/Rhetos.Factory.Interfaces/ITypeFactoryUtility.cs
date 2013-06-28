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
using System.Diagnostics.Contracts;

namespace Rhetos.Factory
{
    public static class ITypeFactoryUtility
    {
        public static T Resolve<T>(this ITypeFactory tf)
        {
            return (T)tf.CreateInstance(typeof(T));
        }

        public static T CreateInstance<T>(this ITypeFactory tf)
        {
            return (T)tf.CreateInstance(typeof(T));
        }

        public static T CreateInstance<T>(this ITypeFactory tf, Type type)
        {
            return (T)tf.CreateInstance(type);
        }

        public static void RegisterType(this ITypeFactory tf, Type type)
        {
            Contract.Requires(type != null);

            tf.Register(new TypeFactoryBuilderHelper(new[] { type }));
        }

        public static void RegisterInstance<T>(this ITypeFactory tf, T instance)
        {
            Contract.Requires(instance != null);

            var tfb = new TypeFactoryBuilderHelper();
            tfb.RegisterInstance(instance);

            tf.Register(tfb);
        }

        public static void RegisterTypes(this ITypeFactory tf, IEnumerable<Type> types)
        {
            tf.Register(new TypeFactoryBuilderHelper(new List<Type>(types ?? Enumerable.Empty<Type>())));
        }

        public static void RegisterFunc<T>(this ITypeFactory tf, Func<ITypeFactory, T> func)
        {
            Contract.Requires(func != null);

            var tfb = new TypeFactoryBuilderHelper();
            tfb.RegisterFunc(func);

            tf.Register(tfb);
        }

        public static void RegisterModule(this ITypeFactory tf, Autofac.Module autofacModule)
        {
            var tfb = new TypeFactoryBuilderHelper();
            tfb.RegisterModule(autofacModule);

            tf.Register(tfb);
        }

    }

}
