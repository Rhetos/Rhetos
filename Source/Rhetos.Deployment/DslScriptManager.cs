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
using System.IO;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Rhetos.Utilities;
using Rhetos.Dsl;

namespace Rhetos.Deployment
{
    public static class DslScriptManager
    {
        public static int UploadDslScriptsToServer(string dslRootFolder, ISqlExecuter sqlExecuter)
        {
            var dsls = LoadDslsFromDisk(dslRootFolder);
            UploadDslScriptsToServer(dsls, sqlExecuter);
            return dsls.Count();
        }

        private static IEnumerable<DslScript> LoadDslsFromDisk(string path)
        {
            path = Path.GetFullPath(path);
            if (path.Last() != '\\') path += '\\';

            var files = Directory.GetFiles(path, "*.rhe", SearchOption.AllDirectories).ToList();
            List<DslScript> dslScripts = new List<DslScript>(files.Count);

            files.ForEach(file =>
                {
                    var content = File.ReadAllText(file, Encoding.Default);
                    dslScripts.Add(new DslScript { Name = file.Substring(path.Length), Script = content, Path = Path.GetFullPath(file) });
                });

            return dslScripts;
        }

        private static void UploadDslScriptsToServer(IEnumerable<DslScript> dslScripts, ISqlExecuter sqlExecuter)
        {
            List<string> sql = new List<string>();

            sql.Add(Sql.Get("DslScriptManager_Delete"));
            sql.AddRange(dslScripts
                .Select(dslScript => Sql.Format("DslScriptManager_Insert",
                    SqlUtility.QuoteText(dslScript.Name),
                    SqlUtility.QuoteText(dslScript.Script))));

            sqlExecuter.ExecuteSql(sql);
        }
    }
}
