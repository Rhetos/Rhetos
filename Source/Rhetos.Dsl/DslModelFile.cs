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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DslModelFile : IDslModel, IDslModelFile
    {
        private readonly ILogger _performanceLogger;
        private readonly DslContainer _dslContainer;
        private readonly IAssetsOptions _rhetosEnvironment;

        public DslModelFile(
            ILogProvider logProvider,
            DslContainer dslContainer,
            IAssetsOptions assetsOptions)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _dslContainer = dslContainer;
            _rhetosEnvironment = assetsOptions;
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

                        _performanceLogger.Write(sw, "Initialize (" + _dslContainer.Concepts.Count() + " concepts).");
                        _initialized = true;
                    }
        }

        private const string DslModelFileName = "DslModel.json";

        public void SaveConcepts(IEnumerable<IConceptInfo> concepts)
        {
            var sw = Stopwatch.StartNew();

            CsUtility.Materialize(ref concepts);
            _performanceLogger.Write(sw, "SaveConcepts: Materialize.");

            JsonUtility.SerializeToFile(concepts, DslModelFilePath, _serializerSettings);
            _performanceLogger.Write(sw, "SaveConcepts: Serialize and write.");
        }

        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
        };

        private string DslModelFilePath => Path.Combine(_rhetosEnvironment.AssetsFolder, DslModelFileName);

        private IEnumerable<IConceptInfo> LoadConcepts()
        {
            var sw = Stopwatch.StartNew();

            var concepts = JsonUtility.DeserializeFromFile<IEnumerable<IConceptInfo>>(DslModelFilePath, _serializerSettings);
            _performanceLogger.Write(sw, "Load.");
            return concepts;
        }
    }
}
