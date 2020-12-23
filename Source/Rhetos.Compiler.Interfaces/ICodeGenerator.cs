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

using Rhetos.Extensibility;
using System.Collections.Generic;

namespace Rhetos.Compiler
{
    /// <summary>
    /// From DSL model and code generator plugins generates source.
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// For each concept in DslModel executes registered code generator plugin and returns the generated source code.
        /// </summary>
        /// <param name="initialCodeGenerator">Optional. It will be called before other code generators, with IConceptInfo argument set to null.</param>
        string ExecutePlugins<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator;

        /// <summary>
        /// For each concept in DslModel executes registered code generator plugin and returns the generated source code split to files.
        /// </summary>
        /// <param name="initialCodeGenerator">Optional. It will be called before other code generators, with IConceptInfo argument set to null.</param>
        IDictionary<string, string> ExecutePluginsToFiles<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator;
    }
}
