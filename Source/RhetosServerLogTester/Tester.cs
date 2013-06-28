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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using Rhetos;

[assembly: CLSCompliant(true)]
namespace RhetosServerLogTester
{
    public enum ExecResult
    {
        Ok, Error
    }

    public sealed class Tester
    {
        private static readonly string AppName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        public const string Version = "V1.0";
        public static string ErrorFileName
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName + ".lasterr"); }
        }

        private TextWriter TextWriter { get; set; }
        public string ServerName { get; private set; }
        public Testing Testing { get; private set; }

        private IServerApplication _serverApplication;
        public IServerApplication ServerApplication
        {
            get { return _serverApplication ?? (_serverApplication = new ServerProxy(new EndpointAddress(ServerName))); }
            private set { _serverApplication = value; }
        }

        public Tester(TextWriter textWriter, IServerApplication serverApplication = null)
        {
            TextWriter = textWriter;
            ServerName = ConfigurationManager.AppSettings["Server"];
            ServerApplication = serverApplication;
        }

        public ExecResult Exec(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args.Length > 1)
                        ServerName = args[1];
                    Testing = new Testing(args[0]);

                    var newLogFileName = Testing.RunTest(ServerApplication, TextWriter);
                    TextWriter.WriteLine("OK.");
                    TextWriter.Write("Created {0}, {1} and {2}.", newLogFileName, Testing.ProcessAndSave(), new Testing(newLogFileName).ProcessAndSave());
                }
                else
                    TextWriter.Write(Help());

                return ExecResult.Ok;
            }
            catch (Exception e)
            {
                TextWriter.WriteLine("Error: " + e.Message);
                TextWriter.Write("Details in '" + ErrorFileName + "'.");

                File.WriteAllText(ErrorFileName, e.ToString());

                return ExecResult.Error;
            }
        }

        public static string Help()
        {
            var help = new StringBuilder();

            help.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}, usage:", AppName, Version).AppendLine()
                .AppendFormat(CultureInfo.InvariantCulture, "\t{0} <filename> [<server>]", AppName).AppendLine()
                .AppendFormat(CultureInfo.InvariantCulture, "If you omit <server>, it's taken from '{0}'.", AppName + ".exe.config").AppendLine()
                .AppendFormat(CultureInfo.InvariantCulture, "Server is expected to be running and configured (db/dsl/data).");

            return help.ToString();
        }
    }
}
