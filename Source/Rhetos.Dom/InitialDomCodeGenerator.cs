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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Text;

namespace Rhetos.Dom
{
    public class InitialDomCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string RhetosHostBuilderInitialConfigurationTag = "/*RhetosHostBuilder.InitialConfiguration*/";
        public static readonly string RhetosHostBuilderPluginAssembliesTag = "/*RhetosHostBuilder.PluginAssemblies*/";
        public static readonly string RhetosHostBuilderPluginTypesTag = "/*RhetosHostBuilder.PluginTypes*/";

        private readonly PluginInfoCollection _plugins;
        private readonly DatabaseSettings _databaseSettings;

        public InitialDomCodeGenerator(
            PluginInfoCollection plugins,
            DatabaseSettings databaseSettings)
        {
            _plugins = plugins;
            _databaseSettings = databaseSettings;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var addPlugins = new StringBuilder();
            foreach (var plugin in _plugins)
            {
                addPlugins.Append($"typeof({plugin.Type.FullName}),{Environment.NewLine}                ");
            }

            var rhetosHostBuilderCode =
$@"using Autofac;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Rhetos
{{
    public static class RhetosHostBuilderExtensions
    {{
        /// <summary>
        /// Initializes a new instance of <see cref=""RhetosHostBuilder""/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        /// The following defaults are applied:
        /// <list type=""bullet"">
        /// <item>Build-time configuration settings that are hard-coded in runtime environment:
        ///     <see cref=""RhetosAppOptions.RhetosRuntimePath""/> and <see cref=""DatabaseSettings""/></item>.
        /// <item>Registers plugin types from dependent libraries, and the current assembly for additional plugin discovery.</item>
        /// <item>Various plugin packages may add additional configuration settings and components registration.</item>
        /// </list>
        /// </remarks>
        public static IRhetosHostBuilder ConfigureRhetosHostDefaults(this IRhetosHostBuilder hostBuilder)
        {{
            hostBuilder
                .ConfigureConfiguration(containerBuilder => containerBuilder
                    .AddKeyValue(
                        ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosRuntimePath),
                        typeof(RhetosHostBuilderExtensions).Assembly.Location)
                    .AddOptions(new Rhetos.Utilities.DatabaseSettings
                        {{
                            DatabaseLanguage = {CsUtility.QuotedString(_databaseSettings.DatabaseLanguage)},
                        }})
                    )
                .AddPluginAssemblies(GetPluginAssemblies())
                .AddPluginTypes(GetPluginTypes());
            {RhetosHostBuilderInitialConfigurationTag}
            return hostBuilder;
        }}

        private static IEnumerable<Assembly> GetPluginAssemblies()
        {{
            return new Assembly[]
            {{
                typeof(RhetosHostBuilderExtensions).Assembly,
                {RhetosHostBuilderPluginAssembliesTag}
            }};
        }}

        private static IEnumerable<Type> GetPluginTypes()
        {{
            #pragma warning disable CS0618 // (Type or member is obsolete) Obsolete plugins can be registered without a warning, their usage will show a warning.
            return new Type[]
            {{
                {addPlugins}{RhetosHostBuilderPluginTypesTag}
            }};
            #pragma warning restore CS0618
        }}
    }}
}}
";
            codeBuilder.InsertCodeToFile(rhetosHostBuilderCode, "RhetosHostBuilderExtensions");
        }
    }
}
