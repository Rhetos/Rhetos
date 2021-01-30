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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.DatabaseGenerator.Test
{
    public class MockSqlExecuter : ISqlExecuter
    {
        public List<(List<string> Scripts, bool UseTransaction)> ExecutedScriptsWithTransaction { get; private set; } = new List<(List<string> Scripts, bool UseTransaction)>();

        public void ExecuteReader(string command, Action<System.Data.Common.DbDataReader> action)
        {
            throw new NotImplementedException();
        }

        public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
        {
            ExecuteSql(commands, useTransaction, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands, bool useTransaction,
            Action<int> beforeExecute, Action<int> afterExecute)
        {
            ExecutedScriptsWithTransaction.Add((commands.ToList(), useTransaction));
        }
    }
}
