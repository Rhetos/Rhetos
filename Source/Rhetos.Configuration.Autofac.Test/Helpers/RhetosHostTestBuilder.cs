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
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Configuration.Autofac.Test
{
    public class RhetosHostTestBuilder : RhetosHostBuilder
    {
        public RhetosHostTestBuilder()
        {
            AddPluginAssemblies(new[] { GetType().Assembly });
        }

        public static IConfiguration GetBuildConfiguration()
        {
            string rhetosAppRootPath = AppDomain.CurrentDomain.BaseDirectory;

            // This code is mostly copied from Rhetos CLI build-time configuration.

            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddOptions(new RhetosBuildEnvironment
                {
                    ProjectFolder = rhetosAppRootPath,
                    OutputAssemblyName = Assembly.GetEntryAssembly().GetName().Name,
                    CacheFolder = Path.Combine(rhetosAppRootPath, "BuildCacheTest"),
                    GeneratedAssetsFolder = Path.Combine(rhetosAppRootPath, "GeneratedAssetsTest"), // Custom for testing
                    GeneratedSourceFolder = "GeneratedSourceTest",
                })
                .AddOptions(new RhetosTargetEnvironment
                {
                    TargetPath = @"TargetPathTest",
                    TargetAssetsFolder = @"TargetAssetsTest",
                })
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddJsonFile(Path.Combine(rhetosAppRootPath, RhetosBuildEnvironment.ConfigurationFileName), optional: true)
                .Build();

            return configuration;
        }

        public static void GetRuntimeConfiguration(IConfigurationBuilder configurationBuilder)
        {
            string rhetosAppRootPath = AppDomain.CurrentDomain.BaseDirectory;
            string currentAssemblyPath = typeof(AutofacConfigurationTest).Assembly.Location;
            var allOtherAssemblies = Directory.GetFiles(Path.GetDirectoryName(currentAssemblyPath), "*.dll")
                .Except(new[] { currentAssemblyPath })
                .Select(path => Path.GetFileName(path))
                .ToList();

            // Simulating common run-time configuration of Rhetos CLI.
            configurationBuilder
                .AddKeyValue(ConfigurationProvider.GetKey((DatabaseOptions o) => o.SqlCommandTimeout), 0)
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddJsonFile(Path.Combine(rhetosAppRootPath, "rhetos-app.settings.json"))
                .AddJsonFile(Path.Combine(rhetosAppRootPath, DbUpdateOptions.ConfigurationFileName), optional: true)
                // shortTransactions
                .AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.ShortTransactions), true)
                // skipRecompute
                .AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.SkipRecompute), true)
                .AddOptions(new RhetosAppOptions
                {
                    RhetosHostFolder = Path.GetDirectoryName(currentAssemblyPath),
                    RhetosAppAssemblyName = Path.GetFileNameWithoutExtension(currentAssemblyPath)
                })
                .AddOptions(new PluginScannerOptions
                {
                    // Ignore other MEF plugins from assemblies that might get bundled in the same testing output folder.
                    IgnoreAssemblyFiles = allOtherAssemblies
                });
        }
    }
}
