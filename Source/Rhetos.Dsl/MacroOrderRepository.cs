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

namespace Rhetos.Dsl
{
    public class MacroOrder
    {
        public string EvaluatorName;
        public decimal EvaluatorOrder;
    }

    /// <summary>
    /// Reads from database the recommended order of macro concepts evaluation.
    /// The order is optimized to reduce number of iteration in macro evaluation.
    /// </summary>
    public class MacroOrderRepository : IMacroOrderRepository
    {
        ISqlExecuter _sqlExecuter;
        ILogger _loadOrderLogger;
        ILogger _saveOrderLogger;

        public MacroOrderRepository(ISqlExecuter sqlExecuter, ILogProvider logProvider)
        {
            _sqlExecuter = sqlExecuter;
            _loadOrderLogger = logProvider.GetLogger("MacroRepositoryLoad");
            _saveOrderLogger = logProvider.GetLogger("MacroRepositorySave");
        }

        /// <summary>
        /// Dictionary's Key is EvaluatorName, Value is EvaluatorOrder.
        /// </summary>
        public List<MacroOrder> Load()
        {
            string sql = "SELECT EvaluatorName, EvaluatorOrder FROM Rhetos.MacroEvaluatorOrder";
            var macroOrders = new List<MacroOrder>();
            _sqlExecuter.ExecuteReader(sql, reader =>
                {
                    macroOrders.Add(new MacroOrder
                    {
                        EvaluatorName = reader.GetString(0),
                        EvaluatorOrder = reader.GetDecimal(1)
                    });
                });
            _loadOrderLogger.Trace(() => "\r\n" + ReportMacroOrders(macroOrders));
            return macroOrders;
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
            _saveOrderLogger.Trace(() => "\r\n" + ReportMacroOrders(macroOrders));
            string sql = string.Join("\r\n", macroOrders.Select(GetSaveSql));
            _sqlExecuter.ExecuteSql(new[] { sql });
        }

        private string GetSaveSql(MacroOrder macroOrder)
        {
            return string.Format(
@"IF NOT EXISTS (SELECT * FROM Rhetos.MacroEvaluatorOrder WHERE EvaluatorName = {0})
	INSERT INTO Rhetos.MacroEvaluatorOrder (EvaluatorName, EvaluatorOrder) VALUES (LEFT({0}, 256), {1});
ELSE
	UPDATE Rhetos.MacroEvaluatorOrder SET EvaluatorOrder = {1} WHERE EvaluatorName = {0};
",
                SqlUtility.QuoteText(macroOrder.EvaluatorName),
                macroOrder.EvaluatorOrder.ToString(CultureInfo.InvariantCulture));
        }
    }
}
