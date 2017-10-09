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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.AspNetFormsAuth
{
    public class InitializeAssemblyResolver
    {
        private readonly string _relativePathToBinFolder;

        /// <summary>
        /// AssemblyResolver needs to be initialized as a static member, to allow other static members' initialization (from Rhetos.Utilities).
        /// </summary>
        public InitializeAssemblyResolver(string relativePathToBinFolder)
        {
            _relativePathToBinFolder = relativePathToBinFolder;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(FindAssemblyInParentFolder);
        }

        /// <summary>
        /// This program is executed in bin\Plugins, so the assemblies from the parent (bin) folder must be loaded manually.
        /// </summary>
        private Assembly FindAssemblyInParentFolder(object sender, ResolveEventArgs args)
        {
            string rhetosBinFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _relativePathToBinFolder));
            string shortAssemblyName = new AssemblyName(args.Name).Name;
            string guessAssemblyFileName = shortAssemblyName + ".dll";
            var files = Directory.GetFiles(rhetosBinFolder, guessAssemblyFileName, SearchOption.AllDirectories);

            if (files.Count() == 1)
            {
                //Console.WriteLine($"[Trace] {AppDomain.CurrentDomain.FriendlyName}: Gussing assembly '{shortAssemblyName}' location: {files.Single()}.");
                return Assembly.LoadFrom(files.Single());
            }
            else if (files.Count() > 1)
            {
                Console.WriteLine($"[Error] {AppDomain.CurrentDomain.FriendlyName}: Found more than one assembly file for '{shortAssemblyName}' inside '{rhetosBinFolder}': {string.Join(", ", files)}.");
                return null;
            }
            else
            {
                Console.WriteLine($"[Error] {AppDomain.CurrentDomain.FriendlyName}: Could not find assembly '{shortAssemblyName}' inside '{rhetosBinFolder}'.");
                return null;
            }
        }
    }
}
