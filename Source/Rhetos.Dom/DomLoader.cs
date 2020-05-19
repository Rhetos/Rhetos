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

using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Dom
{
    public class DomLoader : IDomainObjectModel
    {
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        private List<Assembly> _assemblies;
        private readonly object _assembliesLock = new object();
        private readonly RhetosAppOptions _rhetosAppOptions;

        public DomLoader(ILogProvider logProvider, RhetosAppOptions rhetosAppOptions)
        {
            _logger = logProvider.GetLogger("DomLoader");
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _rhetosAppOptions = rhetosAppOptions;
        }

        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                if (_assemblies == null)
                    lock (_assembliesLock)
                        if (_assemblies == null)
                            _assemblies = LoadObjectModel();

                return _assemblies;
            }
        }

        private List<Assembly> LoadObjectModel()
        {
            var loaded = new List<Assembly>();
            var sw = Stopwatch.StartNew();

            if (Paths.DomAssemblyFiles.All(file => File.Exists(file)))
            {
                foreach (string name in Paths.DomAssemblyFiles.Select(Path.GetFileNameWithoutExtension))
                {
                    _logger.Trace("Loading assembly \"" + name + "\".");
                    var assembly = Assembly.Load(name);
                    if (assembly == null)
                        throw new FrameworkException($"Failed to load assembly '{name}'.");
                    loaded.Add(assembly);
                    _performanceLogger.Write(sw, "LoadObjectModel " + name);
                }
                return loaded;
            }
            else
            {
                return new List<Assembly> { Assembly.Load(Path.GetFileNameWithoutExtension(_rhetosAppOptions.RhetosRuntimePath)) };
            }
        }
    }
}
