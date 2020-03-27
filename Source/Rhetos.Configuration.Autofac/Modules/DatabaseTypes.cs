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

using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.Configuration.Autofac.Modules
{
    internal static class DatabaseTypes
    {
        public static Type GetSqlExecuterType(string databaseLanguage)
        {
            return GetDatabaseTypes(databaseLanguage).SqlExecuter;
        }

        public static Type GetSqlUtilityType(string databaseLanguage)
        {
            return GetDatabaseTypes(databaseLanguage).SqlUtility;
        }

        private static (Type SqlExecuter, Type SqlUtility) GetDatabaseTypes(string databaseLanguage)
        {
            var implementationsByLanguage = new Dictionary<string, (Type SqlExecuter, Type SqlUtility)>
            {
                { "MsSql", (SqlExecuter: typeof(MsSqlExecuter), SqlUtility: typeof(MsSqlUtility) )},
                { "Oracle", (SqlExecuter: typeof(OracleSqlExecuter), SqlUtility: typeof(OracleSqlUtility) )},
            };

            if (implementationsByLanguage.TryGetValue(databaseLanguage, out var databaseTypes))
                return databaseTypes;
            else
                throw new FrameworkException($"Unsupported database language '{SqlUtility.DatabaseLanguage}'. Supported languages are: {string.Join(", ", implementationsByLanguage.Keys)}.");
        }
    }
}
