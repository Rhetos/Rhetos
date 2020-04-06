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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos
{
    public class RhetosProjectContentProvider
    {
        private const string ProjectAssetsFileName = "rhetos-project.assets.json";

        public string ProjectAssetsFilePath { get; }

        private readonly FilesUtility _filesUtility;
        private readonly ILogger _logger;
        private readonly string _projectRootFolder;

        public RhetosProjectContentProvider(string projectRootFolder, ILogProvider logProvider)
        {
            ProjectAssetsFilePath = Path.Combine(projectRootFolder, "obj", "Rhetos", ProjectAssetsFileName);
            _filesUtility = new FilesUtility(logProvider);
            _logger = logProvider.GetLogger(GetType().ToString());
            _projectRootFolder = projectRootFolder;
        }

        public RhetosProjectContent Load()
        {
            if (!File.Exists(ProjectAssetsFilePath))
            {
                if (Directory.Exists(_projectRootFolder) && Directory.EnumerateFiles(_projectRootFolder, "*.csproj").Any())
                    throw new FrameworkException($"Missing file '{ProjectAssetsFileName}' required for build." +
                        $" The project must include Rhetos NuGet package." +
                        $" If manually running Rhetos build, MSBuild should pass first to create this file.");
                else
                    throw new FrameworkException($"Missing file '{ProjectAssetsFilePath}' required for build." +
                        $" Make sure to specify a valid project folder ({_projectRootFolder}).");
            }

            string serialized = File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8);
            var rhetosProjectContent = JsonConvert.DeserializeObject<RhetosProjectContent>(serialized, _serializerSettings);
            return rhetosProjectContent;
        }

        public void Save(RhetosBuildEnvironment rhetosBuildEnvironment, RhetosTargetEnvironment rhetosTargetEnvironment, RhetosProjectAssets rhetosProjectAssets)
        {
            var rhetosProjectContent = new RhetosProjectContent
            {
                RhetosBuildEnvironment = rhetosBuildEnvironment,
                RhetosTargetEnvironment = rhetosTargetEnvironment,
                RhetosProjectAssets = rhetosProjectAssets
            };
            string serialized = JsonConvert.SerializeObject(rhetosProjectContent, _serializerSettings);
            string oldSerializedData = File.Exists(ProjectAssetsFilePath) ? File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8) : "";

            if (!Directory.Exists(Path.GetDirectoryName(ProjectAssetsFilePath)))
                _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(ProjectAssetsFilePath));

            if (oldSerializedData != serialized)
            {
                File.WriteAllText(ProjectAssetsFilePath, serialized, Encoding.UTF8);
                _logger.Info($"{nameof(RhetosProjectAssets)} updated.");
            }
            else
            {
                _logger.Info($"{nameof(RhetosProjectAssets)} is already up-to-date.");
            }
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
    }
}
