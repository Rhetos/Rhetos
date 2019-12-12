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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DslModelFromFile : IDslModel
    {
        private readonly ILogger _performanceLogger;
        private readonly DslContainer _dslContainer;
        private readonly RhetosAppOptions _rhetosAppOptions;

        public DslModelFromFile(
            ILogProvider logProvider,
            DslContainer dslContainer,
            RhetosAppOptions rhetosAppOptions)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _dslContainer = dslContainer;
            _rhetosAppOptions = rhetosAppOptions;
        }

        #region IDslModel implementation

        public IEnumerable<IConceptInfo> Concepts
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _dslContainer.Concepts;
            }
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.FindByKey(conceptKey);
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.GetIndex<T>();
        }

        #endregion

        private bool _initialized;
        private readonly object _initializationLock = new object();

        private void Initialize()
        {
            if (!_initialized)
                lock (_initializationLock)
                    if (!_initialized)
                    {
                        var sw = Stopwatch.StartNew();

                        var loadedConcepts = LoadConcepts();
                        _dslContainer.AddNewConceptsAndReplaceReferences(loadedConcepts);

                        _performanceLogger.Write(sw, "DslModelFile.Initialize (" + _dslContainer.Concepts.Count() + " concepts).");
                        _initialized = true;
                    }
        }

        private const string DslModelFileName = "DslModel.json";

        private IEnumerable<IConceptInfo> LoadConcepts()
        {
            var sw = Stopwatch.StartNew();

            string path = Path.Combine(_rhetosAppOptions.AssetsFolder, DslModelFileName);
            string serializedConcepts = File.ReadAllText(path, Encoding.UTF8);

            var serializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.All,
            };

            var concepts = (IEnumerable<IConceptInfo>)JsonConvert.DeserializeObject(serializedConcepts, serializerSettings);
            _performanceLogger.Write(sw, "DslModelFile.Load.");
            return concepts;
        }
    }
}
