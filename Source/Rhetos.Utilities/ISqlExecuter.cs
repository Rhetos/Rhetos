/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;

namespace Rhetos.Utilities
{
    public interface ISqlExecuter
    {
        void ExecuteReader(string command, Action<DbDataReader> action);
        void ExecuteSql(IEnumerable<string> commands);
    }

    public class NullSqlExecuter : ISqlExecuter
    {
        const string message = "SQL executer in not avaliable.";

        public void ExecuteReader(string command, Action<DbDataReader> action)
        {
            throw new FrameworkException(message);
        }

        public void ExecuteSql(IEnumerable<string> commands)
        {
            throw new FrameworkException(message);
        }
    }
}
