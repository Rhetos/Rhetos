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

        /// <summary>
        /// It the exception is a UserException, this function evaluates the message parameters using string.Format, without localization.
        /// Use this method in error logging to make sure every error is logger even if it's message format is not valid.
        /// </summary>
        public static string SafeFormatUserMessage(Exception ex)
        {
            var userEx = ex as UserException;
            if (userEx == null)
                return ex.Message;
            try
            {
                return string.Format(userEx.Message, userEx.MessageParameters ?? new object[] { });
            }
            catch (Exception ex2)
            {
                return "Invalid error message format. " + ex2.GetType().Name + ": " + ex2.Message + ","
                    + ", Message: " + (ex.Message == null
                        ? "null"
                        : "\"" + ex.Message + "\"")
                    + ", Parameters: " + (userEx.MessageParameters == null
                        ? "null"
                        : userEx.MessageParameters.Length == 0
                            ? "no parameters"
                            : "\"" + string.Join(", ", userEx.MessageParameters) + "\"")
                    + ".";
            }
        }
    }
}
