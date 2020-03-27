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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;

namespace Rhetos.Compiler
{
    /// <summary>
    /// This is a legacy utility for building assemblies from the generated C# source.
    /// Use it for plugins that should work in legacy DeployPackages (build source and DLLs) and Rhetos CLI (build source only).
    /// Plugins that are designed only for Rhetos CLI, should use ISourceWriter instead.
    /// </summary>
    public interface IAssemblyGenerator
    {
        /// <param name="outputAssemblyPath">
        /// DeployPackages will generate corresponding .cs file and DLL with the provided name.
        /// Rhetos CLI will generate only the .cs file, ignoring the provided <paramref name="outputAssemblyPath"/> extension and folder.
        /// </param>
        /// <param name="manifestResources">
        /// Obsolete parameter for legacy plugins.
        /// If provided, Rhetos CLI build command will not consider generated source as a part of the application's source,
        /// instead it will fall back to legacy behavior (DeployPackages) and will generate source and DLL files as assets files.
        /// </param>
        Assembly Generate(IAssemblySource assemblySource, string outputAssemblyPath, IEnumerable<ManifestResource> manifestResources = null);

        [Obsolete("The Generate method with CompilerParameters is no longer supported, options other then OutputAssembly are ignored. Use the IAssemblyGenerator.Generate method with the outputAssembly string parameter.")]
        Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters);
    }
}