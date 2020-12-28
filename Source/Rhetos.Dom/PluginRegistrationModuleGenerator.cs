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
    public class PluginRegistrationModuleGenerator : IGenerator
    {
        private readonly PluginInfoContainer _plugins;

        private readonly ISourceWriter _sourceWriter;

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public PluginRegistrationModuleGenerator(
            ISourceWriter sourceWriter,
            PluginInfoContainer plugins)
        {
            _sourceWriter = sourceWriter;
            _plugins = plugins;
        }

        public void Generate()
        {
            var sb = new StringBuilder();
            foreach (var plugin in _plugins)
            {
                sb.Append($"            pluginList.Add(typeof({plugin.Type.FullName}));" + Environment.NewLine);
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
        public RhetosHostBuilder()
        {{
        }}

        protected override ContainerBuilder CreateContainerBuilder(IConfiguration configuration)
        {{
            return RhetosContainerBuilder.CreateRunTimeContainerBuilder(configuration, this._builderLogProvider, new List<Assembly> {{ Assembly.GetExecutingAssembly() }}, GetPluginTypes());
        }}

        private List<Type> GetPluginTypes()
        {{
            var pluginList = new List<Type>();

{sb}
            return pluginList;
        }}
    }}
}}

";

            _sourceWriter.Add("RhetosHostBuilder.cs", rhetosHostBuilderCode);
        }
    }
}
