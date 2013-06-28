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
using System.Reflection;
using System.Text;
using Rhetos.Factory;

namespace Rhetos.Dom
{
    public abstract class DomProvider : IDomainObjectModel
    {
        public abstract Assembly ObjectModel
        {
            get;
        }

        public Type ResolveType(string name)
        {
            var type = ObjectModel.GetType(name);
            if (type == null)
                throw new FrameworkException("Cannot create object of type '" + name + "'.");
            return type;
        }

        /// <summary>
        /// Inherited class should call this function once to register the types from domain object model.
        /// </summary>
        protected static void RegisterTypes(Assembly objectModel, ITypeFactory typeFactory)
        {
            foreach (Type type in objectModel.GetTypes())
            {
                try
                {
                    if (typeof(Autofac.Module).IsAssignableFrom(type))
                        typeFactory.RegisterModule((Autofac.Module)Activator.CreateInstance(type));
                    else
                        typeFactory.RegisterType(type);
                }
                catch (Exception)
                {
                    throw new FrameworkException(String.Format("Registration of type {0} failed.", type.FullName));
                }
            }
        }
    }
}
