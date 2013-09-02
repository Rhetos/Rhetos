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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Rhetos.Utilities;

namespace Rhetos.Deployment
{
    public static class DeploymentUtility
    {
        public static void WriteError(string msg)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        public static void Touch(FileInfo file)
        {
            var isReadOnly = file.IsReadOnly;
            file.IsReadOnly = false;
            file.LastWriteTime = DateTime.Now;
            file.IsReadOnly = isReadOnly;
        }

        public static void PrepareRhetosDatabase(ISqlExecuter sqlExecuter)
        {
            string rhetosDatabaseScriptResourceName = "Rhetos.Deployment.RhetosDatabase." + SqlUtility.DatabaseLanguage + ".sql";
            var resourceStream = typeof(DeploymentUtility).Assembly.GetManifestResourceStream(rhetosDatabaseScriptResourceName);
            if (resourceStream == null)
                throw new FrameworkException("Cannot find resource '" + rhetosDatabaseScriptResourceName + "'.");
            var sql = new StreamReader(resourceStream).ReadToEnd();

            var sqlScripts = sql.Split(new[] {"\r\nGO\r\n"}, StringSplitOptions.RemoveEmptyEntries).Where(s => !String.IsNullOrWhiteSpace(s));
            sqlExecuter.ExecuteSql(sqlScripts);
        }

        public static string QuoteSqlIdentifier(string sqlIdentifier)
        {
            if (SqlUtility.DatabaseLanguage == "MsSql")
            {
                sqlIdentifier = sqlIdentifier.Replace("]", "]]");
                return "[" + sqlIdentifier + "]";
            }
            throw new FrameworkException("Database language " + SqlUtility.DatabaseLanguage + " not supported.");
        }
    }
}
