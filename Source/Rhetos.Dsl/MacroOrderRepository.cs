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
        private readonly string _cacheFilePath;

        public MacroOrderRepository(RhetosBuildEnvironment buildEnvironment)
        {
            _cacheFilePath = Path.Combine(buildEnvironment.CacheFolder, "MacroOrder.json");
        }

        public List<MacroOrder> Load()
        {
            if (File.Exists(_cacheFilePath))
            {
                return JsonUtility.DeserializeFromFile<List<MacroOrder>>(_cacheFilePath);
            }
            else
            {
                return new List<MacroOrder>();
            }
        }

        public void Save(IEnumerable<MacroOrder> macroOrders)
        {
            JsonUtility.SerializeToFile(macroOrders, _cacheFilePath, Formatting.Indented);
        }
    }
}
