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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.TestCommon
{
    /// <summary>
    /// This interface provides extension methods for invoking private methods of a tested class.
    /// </summary>
    public interface ITestAccessor
    {
    }

    /// <summary>
    /// Helper methods for invoking on private methods of a tested class.
    /// </summary>
    public static class TestAccessorHelpers
    {
        /// <summary>
        /// Invokes the private instance method of the accessor's base class.
        /// </summary>
        public static dynamic Invoke(this ITestAccessor accessor, string methodName, params object[] parameters)
        {
            return accessor.GetType().BaseType
                    .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .InvokeEx(accessor, parameters);
        }

        /// <summary>
        /// Invokes the private static method of the accessor's base class.
        /// </summary>
        public static dynamic Invoke<TBase>(string methodName, params object[] parameters)
        {
            return typeof(TBase)
                .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .InvokeEx(null, parameters);
        }
    }
}
