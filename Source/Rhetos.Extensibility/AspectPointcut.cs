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
using System.Reflection;

namespace Rhetos.Extensibility
{
    public class AspectPointcuts : IAspectPointcuts, IInterceptorSelector
    {
        private readonly List<IAspect> AspectWithPointcuts = new List<IAspect>();

        public IInterceptor[] SelectInterceptors(Type type, System.Reflection.MethodInfo method, IInterceptor[] interceptors)
        {
            return (from p in AspectWithPointcuts
                    where p.IsValidForMethod(type, method)
                    orderby p.Priority
                    select p.Advice as IInterceptor).ToArray();             
        }

        public void RegisterAspect(IAspect aspect)
        {
            if (!AspectWithPointcuts.Contains(aspect))
                AspectWithPointcuts.Add(aspect);
        }
    }
}
