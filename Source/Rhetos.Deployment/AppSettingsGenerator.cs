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

using Newtonsoft.Json.Linq;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Deployment
{
    /// <summary>
    /// Creates or updates Rhetos runtime configuration file (<see cref="RhetosAppEnvironment.ConfigurationFileName"/>)
    /// with essential information on application structure.
    /// </summary>
    public class AppSettingsGenerator : IGenerator
    {
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;
		private readonly RhetosTargetEnvironment _rhetosTargetEnvironment;
		private readonly ILogger _logger;
		private readonly BuildOptions _buildOptions;

        public AppSettingsGenerator(
			RhetosBuildEnvironment rhetosBuildEnvironment,
			RhetosTargetEnvironment rhetosTargetEnvironment,
			ILogProvider logProvider,
			BuildOptions buildOptions)
        {
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
			_rhetosTargetEnvironment = rhetosTargetEnvironment;
			_logger = logProvider.GetLogger(GetType().Name);
			_buildOptions = buildOptions;
        }

        public void Generate()
        {
			if (_buildOptions.GenerateAppSettings)
			{
				var expectedConfiguration = GetExpectedConfiguration();
				_logger.Trace(() => $"Expected configuration: {string.Join(", ", expectedConfiguration.Select(item => $"{item.Path}={item.Value}"))}");

				string configurationFilePath = Path.Combine(_rhetosBuildEnvironment.ProjectFolder, RhetosAppEnvironment.ConfigurationFileName);
				UpdateJsonFile(configurationFilePath, expectedConfiguration);
			}
			else
				_logger.Trace(() => $"Skipped generating {RhetosAppEnvironment.ConfigurationFileName}. Build option {nameof(_buildOptions.GenerateAppSettings)} is '{_buildOptions.GenerateAppSettings}'.");
        }

        private IEnumerable<(string Path, string Value)> GetExpectedConfiguration()
        {
			var configurationItems = new List<(string Path, string Value)>();

			configurationItems.Add(($"{OptionsAttribute.GetConfigurationPath<RhetosAppOptions>()}:{nameof(RhetosAppOptions.RhetosRuntimePath)}",
				FilesUtility.AbsoluteToRelativePath(_rhetosBuildEnvironment.ProjectFolder, _rhetosTargetEnvironment.TargetPath)));

			configurationItems.Add(($"{OptionsAttribute.GetConfigurationPath<RhetosAppOptions>()}:{nameof(RhetosAppOptions.AssetsFolder)}",
				FilesUtility.AbsoluteToRelativePath(_rhetosBuildEnvironment.ProjectFolder, _rhetosTargetEnvironment.TargetAssetsFolder)));

			return configurationItems.Where(item => item.Value != null);
        }

		private void UpdateJsonFile(string configurationFilePath, IEnumerable<(string Path, string Value)> expectedConfiguration)
		{
			var jObject = File.Exists(configurationFilePath)
					? JObject.Parse(File.ReadAllText(configurationFilePath))
					: new JObject();

			bool modifiedProperties = false;

			foreach (var item in expectedConfiguration)
			{
				var parentObject = jObject;
				var currentPositionInfo = new List<string>();
				var pathElements = item.Path.Split(':');

				foreach (string subObjectKey in pathElements.Take(pathElements.Length - 1))
				{
					currentPositionInfo.Add(subObjectKey);

					var subObject = parentObject[subObjectKey];
					if (subObject == null)
					{
						subObject = new JObject();
						parentObject.Add(subObjectKey, subObject);
					}
					else if (!(subObject is JObject))
					{
						throw new FrameworkException($"Cannot update {RhetosAppEnvironment.ConfigurationFileName}." +
							$" JSON token '{string.Join(":", currentPositionInfo)}' should be {JTokenType.Object} instead of {subObject.Type} {subObject.GetType().Name}.");
					}

					parentObject = (JObject)subObject;
				}

				var property = parentObject[pathElements.Last()];
				if (property == null)
				{
					property = new JProperty(pathElements.Last(), item.Value);
					parentObject.Add(property);
					modifiedProperties = true;
				}
				else
				{
					if (property.Type != JTokenType.String || (string)property != item.Value)
					{
						_logger.Warning($"Updating setting for '{item.Path}'" +
							$" from '{property}' to '{item.Value}'" +
							$" in {RhetosAppEnvironment.ConfigurationFileName}.");
						parentObject[pathElements.Last()] = item.Value;
						modifiedProperties = true;
					}
				}
			}

			if (modifiedProperties)
			{
				_logger.Info($"Saving {RhetosAppEnvironment.ConfigurationFileName}.");
				File.WriteAllText(configurationFilePath, jObject.ToString());
			}
			else
			{
				_logger.Trace(() => $"Configuration in {RhetosAppEnvironment.ConfigurationFileName} is up to date.");
			}
		}

		public IEnumerable<string> Dependencies => Array.Empty<string>();
    }
}
