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

using Rhetos;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeployPackages
{
    public static class DeployPackagesArguments
    {
        internal static readonly Dictionary<string, (string description, string configurationPath)> ValidArguments
            = new Dictionary<string, (string info, string configurationPath)>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "/StartPaused", ("Use for debugging with Visual Studio (Attach to Process).", OptionsAttribute.GetConfigurationPath<DeployPackagesOptions>()) },
            { "/Debug", ("Generates unoptimized DLLs for debugging (ServerDom.*.dll, e.g.).", OptionsAttribute.GetConfigurationPath<BuildOptions>()) },
            { "/NoPause", ("Don't pause on error. Use this switch for build automation.", OptionsAttribute.GetConfigurationPath<DeployPackagesOptions>()) },
            { "/IgnoreDependencies", ("Allow installing incompatible versions of Rhetos packages.", OptionsAttribute.GetConfigurationPath<DeployPackagesOptions>()) },
            { "/ShortTransactions", ("Commit transaction after creating or dropping each database object.", OptionsAttribute.GetConfigurationPath<DbUpdateOptions>()) },
            { "/DatabaseOnly", ("Keep old plugins and files in bin\\Generated.", OptionsAttribute.GetConfigurationPath<DeployPackagesOptions>()) },
            { "/SkipRecompute", ("Skip automatic update of computed data with KeepSynchronized.", OptionsAttribute.GetConfigurationPath<DbUpdateOptions>()) }
        };

        public static IConfigurationBuilder AddCommandLineArgumentsWithConfigurationPaths(this IConfigurationBuilder configurationBuilder, string[] args)
        {
            var argsByPath = args
                .Select(arg => (arg, configurationPath: ValidArguments.TryGetValue(arg, out var argInfo) ? argInfo.configurationPath : ""))
                .GroupBy(arg => arg.configurationPath)
                .Select(grouped => (configurationPath: grouped.Key, args: grouped.Select(arg => arg.arg).ToArray()));

            foreach (var argGroup in argsByPath)
                configurationBuilder.AddCommandLineArguments(argGroup.args, "/", argGroup.configurationPath);

            return configurationBuilder;
        }

        public static bool ValidateArguments(string[] args)
        {
            if (args.Contains("/?"))
            {
                ShowHelp();
                return false;
            }

            var invalidArgument = args.FirstOrDefault(arg => !ValidArguments.ContainsKey(arg));
            if (invalidArgument != null)
            {
                ShowHelp();
                throw new FrameworkException($"Unexpected command-line argument: '{invalidArgument}'.");
            }
            return true;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Command-line arguments:");
            foreach (var argument in ValidArguments)
                Console.WriteLine($"{argument.Key.PadRight(20)} {argument.Value.description}");
        }
    }
}
