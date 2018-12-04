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

using Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{
    /// <summary>
    /// NOTE:
    /// This class does not conform to the standard IoC design pattern.
    /// It uses IoC container directly because it needs to handle a special scope control (separate database connections) and error handling.
    /// </summary>
    public class ApplicationInitialization
    {
        public static IEnumerable<Type> GetSortedInitializers(IContainer container)
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var initializers = GetInitializers(container);

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var initNames = initializers.Select(init => init.GetType().FullName).ToList();
            var initDependencies = initializers.SelectMany(init => (init.Dependencies ?? new string[0]).Select(x => Tuple.Create(x, init.GetType().FullName)));
            Graph.TopologicalSort(initNames, initDependencies);

            var sortedInitializers = initializers.ToArray();
            Graph.SortByGivenOrder(sortedInitializers, initNames.ToArray(), init => init.GetType().FullName);
            return sortedInitializers.Select(initializer => initializer.GetType()).ToList();
        }

        private static IEnumerable<IServerInitializer> GetInitializers(IContainer container)
        {
            return container.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();
        }

        /// <summary>
        /// NOTE:
        /// This method does not conform to the standard IoC design pattern.
        /// It uses IoC container directly because it needs to handle a special scope control (separate database connections) and error handling.
        /// </summary>
        public static void ExecuteInitializer(IContainer container, Type initializerType)
        {
            var deployPackagesLogger = container.Resolve<ILogProvider>().GetLogger("DeployPackages");
            
            Exception originalException = null;
            try
            {
                using (var initializerScope = container.BeginLifetimeScope())
                    try
                    {
                        deployPackagesLogger.Trace("Initialization " + initializerType.Name + ".");
                        var initializers = initializerScope.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();
                        IServerInitializer initializer = initializers.Single(i => i.GetType() == initializerType);
                        initializer.Initialize();
                    }
                    catch (Exception ex)
                    {
                        // Some exceptions result with invalid SQL transaction state that results with another exception on disposal of this 'using' block.
                        // The original exception is logged here to make sure that it is not overridden;
                        originalException = ex;
                        initializerScope.Resolve<IPersistenceTransaction>().DiscardChanges();
                        ExceptionsUtility.Rethrow(ex);
                    }
            }
            catch (Exception ex)
            {
                if (originalException != null && ex != originalException)
                {
                    deployPackagesLogger.Error("Error on cleanup: " + ex.ToString());
                    ExceptionsUtility.Rethrow(originalException);
                }
                else
                    ExceptionsUtility.Rethrow(ex);
            }
        }
    }
}
