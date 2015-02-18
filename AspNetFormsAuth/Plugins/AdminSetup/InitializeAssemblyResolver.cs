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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AdminSetup
{
    /// <summary>
    /// AssemblyResolver needs to be initialized as a static member, to allow other static memebers' initialization (from Rhetos.Utilities).
    /// </summary>
    class InitializeAssemblyResolver
    {
        public InitializeAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(FindAssemblyInParentFolder);
        }

        /// <summary>
        /// This program is executed in bin\Plugins, so the assemblies from the parent (bin) folder must be loaded manually.
        /// </summary>
        private static System.Reflection.Assembly FindAssemblyInParentFolder(object sender, ResolveEventArgs args)
        {
            var rhetosBinFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
            string assemblyPath = Path.GetFullPath(Path.Combine(rhetosBinFolder, new AssemblyName(args.Name).Name + ".dll"));
            if (File.Exists(assemblyPath) == false)
            {
                Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + ": Guessed external assembly path '" + assemblyPath + "' for assembly name '" + args.Name + "'.");
                return null;
            }
            return Assembly.LoadFrom(assemblyPath);
        }
    }
}
