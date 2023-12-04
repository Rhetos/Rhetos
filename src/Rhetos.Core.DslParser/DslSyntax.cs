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

namespace Rhetos.Dsl
{
    /// <summary>
    /// Complete specification of Rhetos DSL syntax for the current Rhetos app,
    /// including <strong>custom DSL extension</strong> for the current application's plugins.
    /// </summary>
    public class DslSyntax
    {
        /// <summary>
        /// DSL syntax version used by the Rhetos app from which the DSL syntax is create or loaded.
        /// </summary>
        /// <remarks>
        /// This version may be different from <see cref="CurrentVersion"/> when using Rhetos libraries
        /// in a tool that loads DslSyntax from a given Rhetos app (for example, DSL IntelliSense).
        /// Tools such as DSL IntelliSense should report a warning or an error if the application's DSL version
        /// is larger than the version supported by currently used libraries (<see cref="CurrentVersion"/>),
        /// and suggest to use a newer version of the tool.
        /// It is expected that the new version will support previous DSL versions.
        /// </remarks>
        public Version Version { get; set; }

        /// <summary>
        /// DSL syntax version supported by the currently loaded Rhetos libraries.
        /// </summary>
        /// <remarks>
        /// See <see cref="Version"/> for more info.
        /// </remarks>
        public static readonly Version CurrentVersion = new Version(6, 0);

        /// <summary>
        /// Version of Rhetos framework (information only).
        /// </summary>
        /// <remarks>
        /// Semantic Versioning 2.0.0 format.
        /// </remarks>
        public string RhetosVersion { get; set; }

        /// <summary>
        /// Value is initially configured from BuildOptions class.
        /// It is persisted as a part of <see cref="DslSyntax"/> to be available to the external language server.
        /// </summary>
        public ExcessDotInKey ExcessDotInKey { get; set; }

        /// <summary>
        /// Value is initially configured from DatabaseSettings class.
        /// It is persisted as a part of <see cref="DslSyntax"/> to be available to the external language server.
        /// </summary>
        public string DatabaseLanguage { get; set; }

        public List<ConceptType> ConceptTypes { get; set; }
    }
}
