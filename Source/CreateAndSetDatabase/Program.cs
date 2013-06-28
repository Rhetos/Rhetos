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
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreateAndSetDatabase
{
    public static class FileReplaceHelper
    {
        private static Encoding[] supportedEncodings = new[]
            {
                Encoding.UTF32,
                Encoding.BigEndianUnicode,
                Encoding.UTF8,
                Encoding.Unicode
            };

        private static Encoding GetFileEncoding(string fileName)
        {
            using (System.IO.FileStream file = new System.IO.FileStream(fileName,
                FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] bom = new byte[4];
                file.Read(bom, 0, 4);
                foreach (var encoding in supportedEncodings.OrderByDescending(e => e.GetPreamble().Length))
                {
                    var encBom = encoding.GetPreamble();
                    var fileBom = bom.Take(encBom.Length).ToArray();
                    bool sameBom = fileBom.Zip(encBom, (f, e) => new { f, e }).All(pair => pair.e == pair.f);
                    if (sameBom) return encoding;
                }
                return Encoding.Default;
            }
        }
        
        public static void ReplaceWithRegex(string fileName, string regex, string value, string invalidMessage)
        {
            var replaceRegex = new Regex(regex, RegexOptions.Multiline);
            var encoding = GetFileEncoding(fileName);
            string fileText = File.ReadAllText(fileName, Encoding.Default);

            var match = replaceRegex.Match(fileText);
            if (match.Success)
                fileText = replaceRegex.Replace(fileText, value, 1);
            else
                throw new Exception("Invalid file. " + invalidMessage);

            File.WriteAllText(fileName, fileText, encoding);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: CreateAndSetDatabase <SQLServer> <DataBaseName>");
                return;
            }
            string sqlMasterConnectionString = "server=" + args[0] + ";database=master;integrated security=SSPI;";
            string dbName = args[1];

            SqlConnection sc = new SqlConnection(sqlMasterConnectionString);
            sc.Open();
            SqlCommand com = new SqlCommand("CREATE DATABASE " + dbName, sc);
            Console.Write("Preparing database ...");
            try
            {
                com.ExecuteNonQuery();
                Console.Write(" database [" + args[0] + "." + dbName + "] created.");
            }
            catch (SqlException se) {
                if (!se.Message.Contains("Database '" + dbName+ "' already exists"))
                    throw se;
                Console.Write(" using existed database.");
            }
            sc.Close();
            Console.WriteLine();

            if (!File.Exists(@"ConnectionStrings.config"))
                File.Copy(@"Template.ConnectionStrings.config", @"ConnectionStrings.config");

            Console.WriteLine(@"Writing connection string in ""ConnectionStrings.config""");
            // set Rhetos to point to new database
            FileReplaceHelper.ReplaceWithRegex(@"ConnectionStrings.config"
                            , @"<add.*?name=""ServerConnectionString""(.|\n)*?/>"
                            , @"<add connectionString=""Data Source=" + args[0] + ";Initial Catalog=" + args[1] + @";Integrated Security=SSPI;"" name=""ServerConnectionString"" providerName=""Rhetos.MsSql"" />"
                            , "Not valid ConnectionStrings.config file.");
        }
    }
}
