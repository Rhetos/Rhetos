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

using System.Collections.Generic;
using System.IO;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Runtime configuration settings.
    /// </summary>
    [Options("Rhetos:App")]
    public class RhetosAppOptions : IAssetsOptions
    {
        /// <summary>
        /// Main Rhetos application's assembly file that is used for bootstrapping Rhetos application via IRhetosHostBuilder implementation.
        /// The value is configured automatically by Rhetos build if <see cref="BuildOptions.GenerateAppSettings"/> is enabled.
        /// </summary>
        [AbsolutePathOption]
        public string RhetosRuntimePath { get; set; }

        /// <summary>
        /// Run-time assets folder.
        /// The value is configured automatically by Rhetos build if <see cref="BuildOptions.GenerateAppSettings"/> is enabled.
        /// </summary>
        [AbsolutePathOption]
        public string AssetsFolder { get; set; }

        public bool EntityFrameworkUseDatabaseNullSemantics { get; set; } = true;

        public double AuthorizationCacheExpirationSeconds { get; set; } = 30;

        public bool AuthorizationAddUnregisteredPrincipals { get; set; } = false;
    }
}
