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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Rhetos.Deployment
{
    public class DeploymentConfiguration
    {
        private readonly string _rootPath;
        private readonly ILogger _logger;
        private readonly Lazy<IEnumerable<PackageRequest>> _packageRequests;
        private readonly Lazy<IEnumerable<PackageSource>> _packageSources;

        public DeploymentConfiguration(RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider)
        {
            _rootPath = rhetosAppEnvironment.RootPath;
            _logger = logProvider.GetLogger(GetType().Name);
            _packageRequests = new Lazy<IEnumerable<PackageRequest>>(LoadPackageRequest);
            _packageSources = new Lazy<IEnumerable<PackageSource>>(LoadPackageSources);
        }

        public IEnumerable<PackageRequest> PackageRequests => _packageRequests.Value;

        public IEnumerable<PackageSource> PackageSources => _packageSources.Value;

        private List<PackageRequest> LoadPackageRequest()
        {
            const string configFileUsage = "Edit the file to specify which Rhetos packages will be deployed. Note that removing a package from the file will uninstall the package.";
            string xml = ReadConfigFile(PackagesConfigurationFileName, PackagesConfigurationTemplateFileName, configFileUsage);
            var xdoc = XDocument.Parse(xml);
            var requests = xdoc.Root.Elements().Select(packageXml =>
                new PackageRequest
                {
                    Id = (string)(packageXml.Attribute("id")),
                    VersionsRange = (string)(packageXml.Attribute("version")),
                    Source = (string)(packageXml.Attribute("source")),
                    RequestedBy = "configuration file " + PackagesConfigurationFileName,
                }).ToList();

            if (requests.Any(r => string.IsNullOrEmpty(r.Id)))
                throw new UserException($"Invalid configuration file format '{PackagesConfigurationFileName}'. Missing attribute 'id'.");
            if (requests.Count == 0)
                _logger.Info($"Warning: No packages specified in '{PackagesConfigurationFileName}'. {configFileUsage}");
            Version dummy;
            // Simple version format in config file will be converted to a specific version "[ver,ver]", instead of being used as a minimal version (as in NuGet dependencies) in order to conform to NuGet packages.config convention.
            foreach (var request in requests)
                if (request.VersionsRange != null && Version.TryParse(request.VersionsRange, out dummy))
                    request.VersionsRange = string.Format("[{0},{0}]", request.VersionsRange);
            return requests;
        }

        private List<PackageSource> LoadPackageSources()
        {
            const string configFileUsage = "Edit the new file to add locations where Rhetos packages can be found.";
            string xml = ReadConfigFile(SourcesConfigurationFileName, SourcesConfigurationTemplateFileName, configFileUsage);
            var xdoc = XDocument.Parse(xml);
            var sources = xdoc.Root.Elements()
                .Select(sourceXml => new PackageSource(_rootPath, sourceXml.Attribute("location").Value))
                .ToList();

            if (sources.Count == 0)
                _logger.Info("No sources defined in '" + SourcesConfigurationFileName + "'. " + configFileUsage);
            return sources;
        }

        /// <summary>The file is placed in GetConfigurationFolder().</summary>
        public const string PackagesConfigurationFileName = "RhetosPackages.config";
        private const string PackagesConfigurationTemplateFileName = "Template.RhetosPackages.config";

        /// <summary>The file is placed in GetConfigurationFolder().</summary>
        public const string SourcesConfigurationFileName = "RhetosPackageSources.config";
        private const string SourcesConfigurationTemplateFileName = "Template.RhetosPackageSources.config";

        private string ReadConfigFile(string configFileName, string templateFileName, string configFileUsage)
        {
            string configFilePath = Path.Combine(_rootPath, configFileName);

            if (File.Exists(configFilePath))
                return File.ReadAllText(configFilePath, Encoding.UTF8);
            else
                throw new UserException($"Missing configuration file '{configFilePath}'." +
                    $" Please copy the template file '{templateFileName}' to '{Path.GetFileName(configFilePath)}'." +
                    $" {configFileUsage}");
        }
    }
}
