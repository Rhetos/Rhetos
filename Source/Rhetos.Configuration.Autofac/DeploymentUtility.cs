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

using Autofac.Core;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;

namespace Rhetos
{
    public static class DeploymentUtility
    {
        public static void PrintErrorSummary(Exception ex)
        {
            while (ex is DependencyResolutionException && ex.InnerException != null)
                ex = ex.InnerException;

            Console.WriteLine();
            Console.WriteLine("=============== ERROR SUMMARY ===============");
            Console.WriteLine(ex.GetType().Name + ": " + ExceptionsUtility.MessageForLog(ex));
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }

        public static void PrintCanonicalError(DslSyntaxException dslException)
        {
            string origin = dslException.FilePosition?.CanonicalOrigin ?? "Rhetos DSL";
            string canonicalError = $"{origin}: error {dslException.ErrorCode ?? "RH0000"}: {dslException.Message.Replace('\r', ' ').Replace('\n', ' ')}";

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(canonicalError);
            Console.ForegroundColor = oldColor;
        }
    }
}
