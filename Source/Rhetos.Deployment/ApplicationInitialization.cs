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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{
    public class ApplicationInitialization
    {
        private readonly ILogger _deployPackagesLogger;
        private readonly IPluginsContainer<IServerInitializer> _initializersContainer;

        public ApplicationInitialization(
            ILogProvider logProvider,
            IPluginsContainer<IServerInitializer> initializersContainer)
        {
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _initializersContainer = initializersContainer;
        }

        public void ExecuteInitializers()
        {
            {
                var initializers = GetSortedInitializers();
                foreach (var initializer in initializers)
                {
                    _deployPackagesLogger.Trace("Initialization " + initializer.GetType().Name + ".");
                    initializer.Initialize();
                }
                if (!initializers.Any())
                    _deployPackagesLogger.Trace("No server initialization plugins.");
            }

            {
                var configFile = new FileInfo(Paths.RhetosServerWebConfigFile);
                if (configFile.Exists)
                {
                    Touch(configFile);
                    _deployPackagesLogger.Trace("Updated " + configFile.Name + " modification date to restart server.");
                }
                else
                    _deployPackagesLogger.Trace("Missing " + configFile.Name + ".");
            }
        }

        private IList<IServerInitializer> GetSortedInitializers()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var initializers = _initializersContainer.GetPlugins();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var initNames = initializers.Select(init => init.GetType().FullName).ToList();
            var initDependencies = initializers.SelectMany(init => (init.Dependencies ?? new string[0]).Select(x => Tuple.Create(x, init.GetType().FullName)));
            Rhetos.Utilities.Graph.TopologicalSort(initNames, initDependencies);

            var sortedInitializers = initializers.ToArray();
            Graph.SortByGivenOrder(sortedInitializers, initNames.ToArray(), init => init.GetType().FullName);
            return sortedInitializers;
        }

        private static void Touch(FileInfo file)
        {
            var isReadOnly = file.IsReadOnly;
            file.IsReadOnly = false;
            file.LastWriteTime = DateTime.Now;
            file.IsReadOnly = isReadOnly;
        }
    }
}
