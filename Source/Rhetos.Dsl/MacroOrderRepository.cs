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
using Rhetos.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rhetos.Dsl
{
    public class MacroOrder
    {
        public string EvaluatorName { get; set; }
        public decimal EvaluatorOrder { get; set; }
    }

    /// <summary>
    /// Reads from file the recommended order of macro concepts evaluation.
    /// The order is optimized to reduce number of iteration in macro evaluation.
    /// </summary>
    public class MacroOrderRepository : IMacroOrderRepository
    {
        private const string MacroOrderFileName = "MacroOrder.json";
        private readonly BuildOptions _buildOptions;
        private readonly FilesUtility _filesUtility;

        public MacroOrderRepository(BuildOptions buildOptions, FilesUtility filesUtility)
        {
            _buildOptions = buildOptions;
            _filesUtility = filesUtility;
        }

        private string CacheFilePath => Path.Combine(_buildOptions.CacheFolder, nameof(MacroOrder), MacroOrderFileName);

        public List<MacroOrder> Load()
        {
            if (File.Exists(CacheFilePath))
            {
                var serializedConcepts = File.ReadAllText(CacheFilePath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<MacroOrder>>(serializedConcepts);
            }
            else
            {
                return new List<MacroOrder>();
            }
        }

        public void Save(IEnumerable<MacroOrder> macroOrders)
        {
            string serializedConcepts = JsonConvert.SerializeObject(macroOrders, Formatting.Indented);
            _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(CacheFilePath));
            File.WriteAllText(CacheFilePath, serializedConcepts, Encoding.UTF8);
        }
    }
}
