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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public static class RhetosAppEnvironmentProvider
    {
        private const string RhetosAppEnvironmentFileName = "RhetosAppEnvironment.json";

        /// <summary>
        /// Saving build-time configuration to be used later at run-time.
        /// </summary>
        public static void Save(RhetosAppEnvironment rhetosAppEnvironment)
        {
            var environmentFolders = rhetosAppEnvironment.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(property => property.Name, property => (string)property.GetValue(rhetosAppEnvironment));

            string saveFolder = rhetosAppEnvironment.RootFolder;
            foreach (var name in environmentFolders.Keys.ToList())
                environmentFolders[name] = FilesUtility.AbsoluteToRelativePath(saveFolder, environmentFolders[name]);

            string serialized = JsonConvert.SerializeObject(environmentFolders, Formatting.Indented);
            string filePath = Path.Combine(saveFolder, RhetosAppEnvironmentFileName);
            File.WriteAllText(filePath, serialized, Encoding.UTF8);
        }

        /// <summary>
        /// Loading application environment configuration at run-time.
        /// </summary>
        public static RhetosAppEnvironment Load(string rhetosAppRootPath)
        {
            rhetosAppRootPath = Path.GetFullPath(rhetosAppRootPath); // For better error reporting.

            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            if (!File.Exists(filePath))
                throw new FrameworkException($"Missing file '{RhetosAppEnvironmentFileName}' in folder '{rhetosAppRootPath}' or subfolders." +
                    $" Please verify that the specified folder contains a valid Rhetos application, and that the Rhetos build have passed successfully.");

            var serialized = File.ReadAllText(filePath, Encoding.UTF8);
            var environmentFolders = JsonConvert.DeserializeObject<Dictionary<string, string>>(serialized);

            string saveFolder = Path.GetDirectoryName(filePath);
            foreach (var name in environmentFolders.Keys.ToList())
                environmentFolders[name] = FilesUtility.RelativeToAbsolutePath(saveFolder, environmentFolders[name]);

            var rhetosAppEnvironment = new RhetosAppEnvironment();
            foreach (var property in rhetosAppEnvironment.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                if (environmentFolders.ContainsKey(property.Name))
                    property.SetValue(rhetosAppEnvironment, environmentFolders[property.Name]);

            return rhetosAppEnvironment;
        }

        public static bool IsRhetosApplicationRootFolder(string rhetosAppRootPath)
        {
            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            return File.Exists(filePath);
        }
    }
}
