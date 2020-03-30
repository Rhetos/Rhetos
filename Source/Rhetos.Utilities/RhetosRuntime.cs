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

namespace Rhetos
{
    public static class Host
    {
        public static IRhetosRuntime Find(string rootFolder)
        {
            var supportedExtensions = new[] { ".dll", ".exe" };
            var hostAssemblies = Directory.GetFiles(rootFolder).Where(x => supportedExtensions.Contains(Path.GetExtension(x)));
            var searchForAssembliesDelegate = GetSearchForAssemblyDelegate(hostAssemblies.ToArray());

            AppDomain.CurrentDomain.AssemblyResolve += searchForAssembliesDelegate;

            var rhetosRuntimeTypes = new List<Type>();
            foreach (var assemblyPath in hostAssemblies)
            {
                var assembly = Assembly.Load(Path.GetFileNameWithoutExtension(assemblyPath));
                rhetosRuntimeTypes.AddRange(assembly.GetTypes().Where(p => typeof(IRhetosRuntime).IsAssignableFrom(p) && !p.IsInterface));
            }

            AppDomain.CurrentDomain.AssemblyResolve -= searchForAssembliesDelegate;

            if (rhetosRuntimeTypes.Count == 0)
                throw new Rhetos.FrameworkException($"No implementation of interface {nameof(IRhetosRuntime)} found."); ;

            if (rhetosRuntimeTypes.Count > 1)
                throw new Rhetos.FrameworkException($"Found multiple implementation of the type {nameof(IRhetosRuntime)}.");

            var rhetosRuntimeInstance = Activator.CreateInstance(rhetosRuntimeTypes.First()) as IRhetosRuntime;

            return rhetosRuntimeInstance;
        }

        private static ResolveEventHandler GetSearchForAssemblyDelegate(params string[] assemblyList)
        {
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                foreach (var assembly in assemblyList.Where(x => Path.GetFileNameWithoutExtension(x) == new AssemblyName(args.Name).Name))
                {
                    if (File.Exists(assembly))
                        return Assembly.LoadFrom(assembly);
                }
                return null;
            });
        }
    }
}
