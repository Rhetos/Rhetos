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
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Dom
{
    public class RhetosHostBuilderGenerator : IGenerator
    {
        public static readonly string PluginAssembliesTag = "/*RhetosHostBuilder.PluginAssemblies*/";
        public static readonly string PluginTypesTag = "/*RhetosHostBuilder.PluginTypes*/";

        private readonly PluginInfoCollection _plugins;

        private readonly ISourceWriter _sourceWriter;

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public RhetosHostBuilderGenerator(
            ISourceWriter sourceWriter,
            PluginInfoCollection plugins)
        {
            _sourceWriter = sourceWriter;
            _plugins = plugins;
        }

        public void Generate()
        {
            var addPlugins = new StringBuilder();
            foreach (var plugin in _plugins)
            {
                addPlugins.Append($"typeof({plugin.Type.FullName}),{Environment.NewLine}                ");
            }

            var rhetosHostBuilderCode = $@"using Autofac;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Rhetos
{{
    public class RhetosHostBuilder : RhetosHostBuilderBase
    {{
        protected override ContainerBuilder CreateContainerBuilder(IConfiguration configuration)
        {{
            var pluginScanner = new Rhetos.Extensibility.RuntimePluginScanner(GetPluginAssemblies(), GetPluginTypes(), _builderLogProvider);
            return new RhetosContainerBuilder(configuration, _builderLogProvider, pluginScanner);
        }}

        private static IEnumerable<Assembly> GetPluginAssemblies()
        {{
            return new Assembly[]
            {{
                Assembly.GetExecutingAssembly(),
                {PluginAssembliesTag}
            }};
        }}

        private static IEnumerable<Type> GetPluginTypes()
        {{
            #pragma warning disable CS0618 // (Type or member is obsolete) Obsolete plugins can be registered without a warning, their usage will show a warning.
            return new Type[]
            {{
                {addPlugins}{PluginTypesTag}
            }};
            #pragma warning restore CS0618
        }}
    }}
}}
";

            _sourceWriter.Add("RhetosHostBuilder.cs", rhetosHostBuilderCode);
        }
    }
}
