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
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace Rhetos.Dom
{
    public enum DomAssemblies { Model, Orm, Repositories };

    public interface IDomainObjectModel
    {
        IEnumerable<Assembly> Assemblies { get; }
    }

    public static class DomainObjectModelExtensions
    {
        public static Type GetType(this IDomainObjectModel dom, string name)
        {
            foreach (Assembly a in dom.Assemblies)
            {
                Type type = a.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static IEnumerable<Type> GetTypes(this IDomainObjectModel dom)
        {
            return dom.Assemblies.SelectMany(assembly => assembly.GetTypes());
        }
    }
}
