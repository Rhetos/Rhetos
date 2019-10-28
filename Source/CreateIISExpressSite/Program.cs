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

        public static void ReplaceWithRegex(string fileName, string regex, string value, string invalidMessage)
        {
            var replaceRegex = new Regex(regex, RegexOptions.Multiline);
            var encoding = GetFileEncoding(fileName);
            string fileText = File.ReadAllText(fileName, Encoding.UTF8);

            var match = replaceRegex.Match(fileText);
            if (match.Success)
                fileText = replaceRegex.Replace(fileText, value, 1);
            else
                throw new Exception("Invalid file. " + invalidMessage + "..." + regex + "...");

            File.WriteAllText(fileName, fileText, encoding);
        }

        public static void ConfigReplaceRegex(string fileName, string regex, string value)
        {
            ReplaceWithRegex(fileName, regex, value, "Unexpected IISExpress.config file format. Copy Template.IISExpress.config.");
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || args.Length > 3)
                {
                    Console.WriteLine("Usage: CreateIISExpressSite <IISSite> <Port> [RhetosAlternativeAppPath]");
                    Console.WriteLine("   Port has to be between 1024 and 65535");
                    return 1;
                }
                string physicalPath = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().LastIndexOf(Path.DirectorySeparatorChar));
                if (args.Length == 3)
                    physicalPath = args[2];
                
                int port = 0;
                if (!Int32.TryParse(args[1], out port))
                    throw new ArgumentException("Port has to be valid integer less than 65536.");
                if (port > 65535)
                    throw new ArgumentException("Port has to be valid integer less than 65536.");

                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
                string iisexpressConfigPath = Path.Combine(appRoot, @"..\IISExpress.config");

                if (!File.Exists(iisexpressConfigPath))
                    File.Copy(Path.Combine(appRoot, @"Template.IISExpress.config"), iisexpressConfigPath);
                Console.Write("Preparing local IISExpress.config ... ");

                bool winAuth = DetectWindowsAuthenticationPlugin(appRoot);

                FileReplaceHelper.ConfigReplaceRegex(iisexpressConfigPath,
                    @"<site name(.|\n)*?</site>",
         @"<site name=""" + args[0] + @""" id=""1"" serverAutoStart=""true"">
                <application path=""/"">
                    <virtualDirectory path=""/"" physicalPath=""" + physicalPath + @""" />
                </application>
                <bindings>
                    <binding protocol=""http"" bindingInformation="":" + port.ToString() + @":localhost"" />
                </bindings>
            </site>");


                FileReplaceHelper.ConfigReplaceRegex(iisexpressConfigPath,
                    @"<!-- AuthenticationPart-->(.|\n)*?<location path(.|\n)*?</location>",
 @"<!-- AuthenticationPart-->
    <location path=""" + args[0] + @""">
        <system.webServer>
            <security>
                <authentication>
                    <anonymousAuthentication enabled=""" + (winAuth ? "false" : "true") + @""" />
                    <windowsAuthentication enabled=""" + (winAuth ? "true" : "false") + @""" />
                </authentication>
            </security>
        </system.webServer>
    </location>");


                Console.WriteLine("DONE");
            }
            catch (ApplicationException ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:");
                Console.WriteLine(ex);
                return 1;
            }
            return 0;
        }

        private static bool DetectWindowsAuthenticationPlugin(string appRoot = "")
        {
            var authenticationPluginSupportsWindowsAuth = new[]
            {
                Tuple.Create("AspNetFormsAuth", Path.Combine(appRoot, @"Plugins\Rhetos.AspNetFormsAuth.dll"), false),
            };

            foreach (var plugin in authenticationPluginSupportsWindowsAuth)
                if (File.Exists(plugin.Item2))
                    return plugin.Item3;
			
			return true; // Windows authentication is enabled by default; no plugins are needed.
        }
    }
}
