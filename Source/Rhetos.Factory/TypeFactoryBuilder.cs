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
    public class TypeFactoryBuilder : ITypeFactoryBuilder
    {
        private readonly List<TypeFactoryBuilderType> Types = new List<TypeFactoryBuilderType>();
        private readonly List<TypeFactoryBuilderInstance> Instances = new List<TypeFactoryBuilderInstance>();
        private readonly List<TypeFactoryBuilderFunc> Funcs = new List<TypeFactoryBuilderFunc>();
        private readonly List<Autofac.Module> Modules = new List<Autofac.Module>();

        public void RegisterBuilderType(TypeFactoryBuilderType type)
        {
            Types.Add(type);
        }

        public void RegisterBuilderInstance(TypeFactoryBuilderInstance instance)
        {
            Instances.Add(instance);
        }

        public void RegisterBuilderModule(Module autofacModule)
        {
            Modules.Add(autofacModule);
        }

        public IEnumerable<TypeFactoryBuilderType> GetRegisteredTypes()
        {
            return Types;
        }

        public IEnumerable<TypeFactoryBuilderInstance> GetRegisteredInstances()
        {
            return Instances;
        }


        public void RegisterBuilderFunc(TypeFactoryBuilderFunc func)
        {
            Funcs.Add(func);
        }

        public IEnumerable<TypeFactoryBuilderFunc> GetRegisteredFuncs()
        {
            return Funcs;
        }

        public IEnumerable<Module> GetRegisteredModules()
        {
            return Modules;
        }
    }
}
