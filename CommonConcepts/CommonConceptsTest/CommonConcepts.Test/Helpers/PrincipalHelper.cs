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

using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test.Helpers
{
    public static class PrincipalHelper
    {
        /// <summary>
        /// These method allows using the generated class Common.Principal, without referencing
        /// additional plugin assemblies with a custom interface that the Common.Principal class could implement.
        /// For example, standard code that calls repository Load or Save would result with the following compiler error
        /// if the Rhetos server contains the 'SimpleSPRTEmail' plugin:
        /// "The type 'IPrincipalWithEmail' is defined in an assembly that is not referenced.You must add a reference to assembly 'Rhetos.AspNetFormsAuth.SimpleSPRTEmail, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null'."
        /// </summary>
        public static GenericRepository<IPrincipal> GenericPrincipal(this Common.ExecutionContext context)
        {
            return context.GenericRepository<IPrincipal>("Common.Principal");
        }

        /// <summary>
        /// These method allows using the generated class Common.Principal, without referencing
        /// additional plugin assemblies with a custom interface that the Common.Principal class could implement.
        /// For example, standard code that calls repository Load or Save would result with the following compiler error
        /// if the Rhetos server contains the 'SimpleSPRTEmail' plugin:
        /// "The type 'IPrincipalWithEmail' is defined in an assembly that is not referenced.You must add a reference to assembly 'Rhetos.AspNetFormsAuth.SimpleSPRTEmail, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null'."
        /// </summary>
        public static IPrincipal InsertPrincipalOrReadId(this Common.ExecutionContext context, string name)
        {
            var generic = GenericPrincipal(context);
            var item = generic.CreateInstance();
            item.Name = name;
            generic.InsertOrReadId(item, p => p.Name);
            return item;
        }
    }
}
