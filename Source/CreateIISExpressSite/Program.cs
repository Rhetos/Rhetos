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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreateIISExpressSite
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

        public static bool MatchesRegex(string fileName, string regex) {
            var replaceRegex = new Regex(regex, RegexOptions.Multiline);
            var encoding = GetFileEncoding(fileName);
            string fileText = File.ReadAllText(fileName, Encoding.Default);
            return replaceRegex.Match(fileText).Success;
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
                throw new Exception("Invalid file. " + invalidMessage + "..." + regex + "...");

            File.WriteAllText(fileName, fileText, encoding);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Usage: CreateIISExpressSite <IISSite> <Port> [RhetosAlternativeAppPath]");
                Console.WriteLine("   Port has to be between 1024 and 65535");
                return;
            }
            string appRoot = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 4);
            if (args.Length == 3)
                appRoot = args[2];

            int port = 0;
            if (!Int32.TryParse(args[1], out port))
                throw new ArgumentException("Port has to be valid integer less than 65536.");
            if (port > 65535)
                throw new ArgumentException("Port has to be valid integer less than 65536.");

            string pathToIISExpress = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IISExpress");
            if (!Directory.Exists(pathToIISExpress))
                throw new Exception("IIS Express is not installed or enabled for current user.");

            if (!File.Exists(@"..\IISExpress.config"))
                File.Copy(@"Template.IISExpress.config", @"..\IISExpress.config");
            Console.Write("Preparing local IISExpress.config ... ");
            FileReplaceHelper.ReplaceWithRegex(@"..\IISExpress.config"
                , @"<site name(.|\n)*?</site>"
                , @"<site name=""" + args[0] + @""" id=""1"" serverAutoStart=""true"">
                <application path=""/"">
                    <virtualDirectory path=""/"" physicalPath=""" + appRoot + @""" />
                </application>
                <bindings>
                    <binding protocol=""http"" bindingInformation="":" + port.ToString() + @":localhost"" />
                </bindings>
            </site>"
                , "Not valid IISExpress.config file.");
            FileReplaceHelper.ReplaceWithRegex(@"..\IISExpress.config"
                , @"<!-- AuthenticationPart-->(.|\n)*?<location path=""(.|\n)*?"">"
                , @"<!-- AuthenticationPart-->" + Environment.NewLine + @"    <location path=""" + args[0] + @""">"
                , "Not valid IISExpress.config file.");
            Console.WriteLine("DONE");
            Console.Write("Setting RhetosService.svc location in web.config ...");
            FileReplaceHelper.ReplaceWithRegex(@"..\web.config"
                , @"<endpoint address=""http(.|\n)*?/RhetosService.svc(.|\n)*?endpoint>"
                , @"<endpoint address=""http://localhost:" + port.ToString() + @"/RhetosService.svc"" binding=""basicHttpBinding"" contract=""Rhetos.IServerApplication""></endpoint>"
                , "Not valid web.config file.");
            Console.WriteLine("DONE");
        }
    }
}
