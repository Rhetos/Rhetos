﻿/*
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

using Rhetos.AspNetFormsAuthWebApi;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using WebMatrix.WebData;

namespace InitAspNetDatabase
{
    class Program
    {
        static InitializeAssemblyResolver initializeAssemblyResolver = new InitializeAssemblyResolver();

        static int Main(string[] args)
        {
            Paths.InitializeRhetosServerRootPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\.."));

            string errorMessage = null;
            try
            {
                CreateMembershipProviderTables();
            }
            catch (ApplicationException ex)
            {
                errorMessage = "CANCELED: " + ex.Message;
            }
            catch (Exception ex)
            {
                errorMessage = "ERROR: " + ex;
            }

            if (errorMessage != null)
            {
                Console.WriteLine();
                Console.WriteLine(errorMessage);
                if (!args.Any(arg => arg.Equals("/nopause")))
                {
                    Console.WriteLine();
                    Console.Write("Press any key to continue . . .");
                    Console.ReadKey(true);
                }
                return 1;
            }

            Console.WriteLine("ASP.NET membership tables created.");
            return 0;
        }

        private static void CreateMembershipProviderTables()
        {
            AuthenticationServiceInitializer.InitializeDatabaseConnection(autoCreateTables: true);

            // Force lazy database initialization.
            int nonexistentUserInt = WebSecurity.GetUserId(Guid.NewGuid().ToString());
            if (nonexistentUserInt != -1)
                throw new ApplicationException("Unexpected GetUserId result.");
        }
    }
}
