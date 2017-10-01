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
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        private List<Assembly> _assemblies;

        public DomLoader(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("DomLoader");
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                if (_assemblies == null)
                    LoadObjectModel();
                return _assemblies;
            }
        }

        private void LoadObjectModel()
        {
            var sw = Stopwatch.StartNew();
            _assemblies = new List<Assembly>();
            foreach (string file in Paths.DomAssemblyFiles)
            {
                _logger.Trace("Loading assembly \"" + file + "\".");
                _assemblies.Add(Assembly.LoadFrom(file));
                _performanceLogger.Write(sw, "DomLoader.LoadObjectModel " + Path.GetFileName(file));
            }
        }
    }
}
