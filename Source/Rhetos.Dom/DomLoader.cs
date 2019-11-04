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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.IO;

namespace Rhetos.Dom
{
    public class DomLoader : IDomainObjectModel
    {
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        private List<Assembly> _assemblies;
        private readonly object _assembliesLock = new object();

        public DomLoader(RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider)
        {
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _logger = logProvider.GetLogger("DomLoader");
            _performanceLogger = logProvider.GetLogger("Performance");
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
            foreach (string name in _rhetosAppEnvironment.DomAssemblyFiles.Select(Path.GetFileNameWithoutExtension))
            {
                _logger.Trace("Loading assembly \"" + name + "\".");
                var assembly = Assembly.Load(name);
                if (assembly == null)
                    throw new FrameworkException($"Failed to load assembly '{name}'.");
                loaded.Add(assembly);
                _performanceLogger.Write(sw, "DomLoader.LoadObjectModel " + name);
            }
            return loaded;
        }
    }
}
