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
using Rhetos.Extensibility;
using Rhetos.Logging;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Extension method which resolves <see cref="ILogProvider"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static ILogProvider GetLogProvider(this ContainerBuilder builder)
        {
            var key = nameof(ILogProvider);
            if (builder.Properties.TryGetValue(key, out var logProvider) && (logProvider is ILogProvider iLogProvider))
                return iLogProvider;

            throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {nameof(ILogProvider)}. " +
                $"This container was probably not created as {nameof(RhetosContainerBuilder)}.");
        }

        /// <summary>
        /// Extension method which resolves <see cref="IPluginScanner"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static IPluginScanner GetPluginScanner(this ContainerBuilder builder)
        {
            var key = nameof(IPluginScanner);
            if (builder.Properties.TryGetValue(key, out var pluginScanner) && (pluginScanner is IPluginScanner iPluginScanner))
                return iPluginScanner;

            throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {nameof(IPluginScanner)}. " +
                $"This container was probably not created as {nameof(RhetosContainerBuilder)}.");
        }

        /// <summary>
        /// Extension method which resolves new <see cref="ContainerBuilderPluginRegistration"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static ContainerBuilderPluginRegistration GetPluginRegistration(this ContainerBuilder builder)
        {
            var pluginScanner = builder.GetPluginScanner();
            var logProvider = builder.GetLogProvider();

            return new ContainerBuilderPluginRegistration(
                builder, logProvider, pluginScanner);
        }
    }
}
