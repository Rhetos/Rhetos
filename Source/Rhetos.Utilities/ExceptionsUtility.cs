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
using System.Reflection;
using System.Text;

namespace Rhetos.Utilities
{
    public static class ExceptionsUtility
    {
        /// <summary>
        /// Same as MethodInfo.Invoke function, but in a case of exception, it will unwrap the TargetInvocationException and rethrow the original one.
        /// </summary>
        public static object InvokeEx(this MethodInfo method, object obj, params object[] parameters)
        {
            try
            {
                return method.Invoke(obj, parameters);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                    Rethrow(ex.InnerException);
                throw;
            }
        }

        /// <summary>
        /// Keeps the original stack trace when rethrowing an existing exception.
        /// </summary>
        public static void Rethrow(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(exception, new object[0]);
            throw exception;
        }
    }
}
