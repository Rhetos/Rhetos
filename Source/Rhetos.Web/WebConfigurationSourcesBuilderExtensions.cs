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

using Rhetos.Utilities.ApplicationConfiguration;
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System;
using System.IO;
using System.Web.Configuration;

namespace Rhetos
{
    public static class WebConfigurationSourcesBuilderExtensions
    {
        /// <summary>
        /// Adds standard configuration from specified web application.
        /// This method is similar to <see cref="ConfigurationSourcesBuilderExtensions.AddConfigurationManagerConfiguration"/> but work on a different application context:
        /// It is needed for utility applications that reference the generated Rhetos applications and use it's runtime components.
        /// This method load's the Rhetos application's configuration, while <see cref="ConfigurationSourcesBuilderExtensions.AddConfigurationManagerConfiguration"/> loads the current utility application's configuration.
        /// When executed from the generated Rhetos application, it should yield same result as <see cref="ConfigurationSourcesBuilderExtensions.AddConfigurationManagerConfiguration"/>.
        /// </summary>
        public static IConfigurationBuilder AddWebConfiguration(this IConfigurationBuilder builder, string webRootPath)
        {
            webRootPath = Path.GetFullPath(webRootPath);
            VirtualDirectoryMapping vdm = new VirtualDirectoryMapping(webRootPath, true);
            WebConfigurationFileMap wcfm = new WebConfigurationFileMap();
            wcfm.VirtualDirectories.Add("/", vdm);
            System.Configuration.Configuration configuration = WebConfigurationManager.OpenMappedWebConfiguration(wcfm, "/");
            builder.Add(new ConfigurationFileSource(configuration));
            return builder;
        }
    }
}
