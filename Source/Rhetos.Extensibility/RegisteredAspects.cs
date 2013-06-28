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
using System.Diagnostics.Contracts;

namespace Rhetos.Extensibility
{
    public class RegisteredAspects
    {
        private readonly List<Type> ForTypes = new List<Type>();
        private readonly List<IInterceptor> Advices = new List<IInterceptor>();
        public AspectPointcuts PointcutSelector { get; private set; }

        public RegisteredAspects()
        {
            PointcutSelector = new AspectPointcuts();
        }

        public void RegisterNewAspect(IAspect aspect/*, Type[] implements*/)
        {
            Contract.Requires(aspect != null);

            /*if (implements != null)
                ForTypes.AddRange((from it in implements
                                   where !ForTypes.Contains(it)
                                   select it).ToArray());
            */
            if (aspect.IsValidForMethod != null)
                PointcutSelector.RegisterAspect(aspect);

            Advices.Add(aspect.Advice);

        }

        public IEnumerable<Type> AllTypes { get { return ForTypes; } }
        public IEnumerable<IInterceptor> AllAdvices { get { return Advices; } }
    }
}
