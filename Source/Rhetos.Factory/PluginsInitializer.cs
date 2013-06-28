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
using System.Diagnostics;
using Rhetos.Extensibility;
using Rhetos.Logging;

namespace Rhetos.Factory
{
    public class PluginsInitializer : IPluginsInitializer
    {
        private readonly ITypeFactory _typeFactory;
        private readonly IPluginRepository<IPluginConfiguration> _pluginRepository;
        private readonly IsInitializedToken _isInitializedToken = new IsInitializedToken();
        private readonly ILogger _performanceLogger;

        public PluginsInitializer(ITypeFactory typeFactory, IPluginRepository<IPluginConfiguration> pluginRepository, ILogProvider logProvider)
        {
            _typeFactory = typeFactory;
            _pluginRepository = pluginRepository;
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public void InitializePlugins()
        {
            lock (_isInitializedToken)
            {
                if (_isInitializedToken.IsGenerated)
                    return;

                var stopwatch = Stopwatch.StartNew();

                var builder = _typeFactory.CreateInstance<ITypeFactoryBuilder>();
                foreach (var plugin in _pluginRepository.GetImplementations())
                {
                    IPluginConfiguration p = (IPluginConfiguration)_typeFactory.CreateInstance(plugin);
                    p.Load(builder);
                }
                _typeFactory.Register(builder);
                _isInitializedToken.IsGenerated = true;

                _performanceLogger.Write(stopwatch, "PluginsInitializer.InitializePlugins");
            }
        }

        private class IsInitializedToken
        {
            public bool IsGenerated = false;
        }
    }
}