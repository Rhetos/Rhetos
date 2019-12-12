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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.IO;
using Rhetos.Extensibility;

namespace Rhetos.Dom
{
    public class DomLoader : IDomainObjectModel
    {
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPluginScanner _pluginScanner;

        private Assembly _domAssembly;
        private readonly object _assembliesLock = new object();

        public DomLoader(RhetosAppOptions rhetosAppOptions, ILogProvider logProvider, IPluginScanner pluginScanner)
        {
            _rhetosAppOptions = rhetosAppOptions;
            _logger = logProvider.GetLogger("DomLoader");
            _performanceLogger = logProvider.GetLogger("Performance");
            _pluginScanner = pluginScanner;
        }

        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                if (_domAssembly == null)
                    lock (_assembliesLock)
                        if (_domAssembly == null)
                            _domAssembly = LoadObjectModel();

                return new List<Assembly> { _domAssembly };
            }
        }

        private Assembly LoadObjectModel()
        {
            var sw = Stopwatch.StartNew();
            //TODO: Check if we need to load the assembly after we have found it
            var domAssembly =  _pluginScanner.FindPlugins("Common.DomRepository").First().Type.Assembly;
            _performanceLogger.Write(sw, "DomLoader.LoadObjectModel " + domAssembly.GetName().Name);
            return domAssembly;
        }
    }
}
