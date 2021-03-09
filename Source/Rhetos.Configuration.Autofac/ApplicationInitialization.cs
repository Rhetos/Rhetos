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
using System.Linq;

namespace Rhetos.Deployment
{
    /// <summary>
    /// NOTE:
    /// This class does not conform to the standard IoC design pattern.
    /// It uses IoC container directly because it needs to handle a special scope control (separate database connections) and error handling.
    /// </summary>
    public static class ApplicationInitialization
    {
        public static Type[] GetSortedInitializers(UnitOfWorkScope scope)
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var initializers = scope.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var initNames = initializers.Select(init => init.GetType().FullName).ToList();
            var initDependencies = initializers.SelectMany(init => (init.Dependencies ?? Array.Empty<string>()).Select(x => Tuple.Create(x, init.GetType().FullName)));
            Graph.TopologicalSort(initNames, initDependencies);

            var sortedInitializers = initializers.ToArray();
            Graph.SortByGivenOrder(sortedInitializers, initNames.ToArray(), init => init.GetType().FullName);
            return sortedInitializers.Select(initializer => initializer.GetType()).ToArray();
        }

        /// <summary>
        /// Note:
        /// This method does not conform to the standard IoC design pattern.
        /// It uses IoC container directly because it needs to handle a special scope control (separate database connections) and error handling.
        /// </summary>
        public static void ExecuteInitializer(RhetosHost container, Type initializerType, ILogProvider logProvider)
        {
            var logger = logProvider.GetLogger(nameof(ApplicationInitialization));
            
            using (var scope = container.CreateScope())
            {
                logger.Info($"Initialization {initializerType.Name}.");
                var initializers = scope.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();
                IServerInitializer initializer = initializers.Single(i => i.GetType() == initializerType);
                initializer.Initialize();
                scope.CommitAndClose();
            }
        }
    }
}
