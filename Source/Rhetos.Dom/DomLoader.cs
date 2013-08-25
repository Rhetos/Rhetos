/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Dom
{
    public class DomLoader : IDomainObjectModel
    {
        private Assembly _objectModel;

        private readonly string _assemblyName;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public DomLoader(
            string assemblyName,
            ILogProvider logProvider)
        {
            _assemblyName = assemblyName;
            _logger = logProvider.GetLogger("DomLoader");
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public Assembly ObjectModel
        {
            get
            {
                if (_objectModel == null)
                    LoadObjectModel();
                return _objectModel;
            }
        }

        private void LoadObjectModel()
        {
            var sw = Stopwatch.StartNew();

            _logger.Trace("Loading assembly by name \"" + _assemblyName + "\".");
            _objectModel = Assembly.Load(new AssemblyName(_assemblyName));
            _logger.Trace("Loaded assembly " + _objectModel.FullName + " at " + _objectModel.Location + ".");

            _performanceLogger.Write(sw, "DomLoader.LoadObjectModel done.");
        }
    }
}
