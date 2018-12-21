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
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Rhetos.Dsl
{
    public class MacroOrder
    {
        public string EvaluatorName;
        public decimal EvaluatorOrder;
    }

    /// <summary>
    /// Reads from file the recommended order of macro concepts evaluation.
    /// The order is optimized to reduce number of iteration in macro evaluation.
    /// </summary>
    public class MacroOrderRepository : IMacroOrderRepository
    {
        ILogger _loadOrderLogger;
        ILogger _saveOrderLogger;
        GeneratedFilesCache _generatedFilesCache;

        public MacroOrderRepository(ILogProvider logProvider, GeneratedFilesCache generatedFilesCache)
        {
            _generatedFilesCache = generatedFilesCache;
            _loadOrderLogger = logProvider.GetLogger("MacroRepositoryLoad");
            _saveOrderLogger = logProvider.GetLogger("MacroRepositorySave");
        }

        private const string MacroOrderFileName = "MacroOrder.json";

        /// <summary>
        /// Dictionary's Key is EvaluatorName, Value is EvaluatorOrder.
        /// </summary>
        public List<MacroOrder> Load()
        {
            var cahceFilePath = Path.Combine(Paths.GeneratedFilesCacheFolder, Path.GetFileNameWithoutExtension(MacroOrderFileName), MacroOrderFileName);
            if (File.Exists(cahceFilePath))
            {
                var serializedConcepts = File.ReadAllText(cahceFilePath);
                return JsonConvert.DeserializeObject<List<MacroOrder>>(serializedConcepts);
            }
            else
            {
                return new List<MacroOrder>();
            }
        }

        private string ReportMacroOrders(IEnumerable<MacroOrder> macroOrders)
        {
            return string.Join("\r\n", macroOrders
                .OrderBy(macro => macro.EvaluatorOrder)
                .Select(macro => macro.EvaluatorName));
        }

        /// <param name="macroOrders">Tuple's Item1 is EvaluatorName, Item2 is EvaluatorOrder.</param>
        public void Save(IEnumerable<MacroOrder> macroOrders)
        {
            string serializedConcepts = JsonConvert.SerializeObject(macroOrders);
            string path = Path.Combine(Paths.GeneratedFolder, MacroOrderFileName);
            File.WriteAllText(path, serializedConcepts, Encoding.UTF8);
        }
    }
}
