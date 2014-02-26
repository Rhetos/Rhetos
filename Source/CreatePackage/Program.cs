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
using System.Text;
using Rhetos.Deployment;

namespace CreatePackage
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Parameters p = ReadParameters(args);

                string fileName = PackageCompiler.CreatePackage(p.PackageRootFolder);
                Console.WriteLine(String.Format("Package created: {0}", fileName));
                return 0;
            }
            catch (ApplicationException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        class Parameters
        {
            public string PackageRootFolder;
        }

        private static Parameters ReadParameters(string[] args)
        {
            if (args.Length == 0)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendLine("");
                msg.AppendLine("Creates Rhetos deployment package from data found in <package root folder>.");
                msg.AppendLine("Specific data must be placed in following sub folders: DslScripts, Resources,");
                msg.AppendLine("DataMigration and Plugins\\ForDeployment (content ends up in the Plugins");
                msg.AppendLine("folder in the package zip file).");
                msg.AppendLine("<package root folder> must contain a valid PackageInfo.xml file.");
                msg.AppendLine("Result zip file name is compiled from the information found in PackageInfo.xml");
                msg.AppendLine("and is placed beside <package root folder>.");
                msg.AppendLine("");
                msg.AppendLine("Usage:");
                msg.AppendLine("CreatePackage.exe <package root folder>");

                throw new ApplicationException(msg.ToString());
            }

            const int expectedArgumentsCount = 1;
            if (args.Length != expectedArgumentsCount)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendLine(String.Format("CreatePackage expects {0} argument, got {1} arguments:", expectedArgumentsCount, args.Length));
                for (int i = 0; i < args.Length; i++)
                    msg.AppendLine(String.Format("{0}: {1}", i + 1, args[i]));

                throw new ApplicationException(msg.ToString());
            }

            return new Parameters
            {
                PackageRootFolder = args[0]
            };
        }
    }
}
