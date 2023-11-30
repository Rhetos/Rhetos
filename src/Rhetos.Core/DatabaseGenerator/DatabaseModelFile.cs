﻿/*
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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    public class DatabaseModelFile
    {
        private const string DatabaseModelFileName = "DatabaseModel.json";

        private readonly ILogger _performanceLogger;
        private readonly string _databaseModelFilePath;

        public DatabaseModelFile(
            ILogProvider logProvider,
            IAssetsOptions assetsOptions)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _databaseModelFilePath = Path.Combine(assetsOptions.AssetsFolder, DatabaseModelFileName);
        }

        public void Save(DatabaseModel databaseModel)
        {
            var stopwatch = Stopwatch.StartNew();
            JsonUtility.SerializeToFile(databaseModel, _databaseModelFilePath, JsonSerializerSettings);
            _performanceLogger.Write(stopwatch, $"{nameof(Save)}: Serialize and write.");
        }

        public DatabaseModel Load()
        {
            var stopwatch = Stopwatch.StartNew();

            if (!File.Exists(_databaseModelFilePath))
            {
                throw new FrameworkException("Cannot update database because the database model was not generated." +
                    " Please check that the build has completed successfully before updating the database.");
            }

            var databaseModel = JsonUtility.DeserializeFromFile<DatabaseModel>(_databaseModelFilePath, JsonSerializerSettings);
            _performanceLogger.Write(stopwatch, $"{nameof(Load)}.");
            return databaseModel;
        }

        JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            Formatting = Formatting.Indented,
        };
    }
}
