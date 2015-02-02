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
using System.IO;
using System.Text;
using System.Threading;
using Rhetos.Deployment;

namespace ExtractPackages
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Parameters p = ReadParameters();

                Console.WriteLine("Reading package list from " + p.PackageSet);
                var packages = PackageSetExtractor.ReadPackageList(p.PackageSet);

                Console.WriteLine("Extracting " + packages.Length + " packages:");
                foreach (string file in packages)
                    Console.WriteLine(" " + Path.GetFileName(file));
                PackageSetExtractor.ExtractAndCombinePackages(packages, p.DeploymentFolder);

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                WriteError(ex.Message);
                Console.WriteLine("Details:");
                Console.WriteLine(ex);
                if (Environment.UserInteractive) 
                    Thread.Sleep(3000);
                return 1;
            }
        }

        class Parameters
        {
            public string DeploymentFolder;
            public string PackageSet;
        }

        private static Parameters ReadParameters()
        {
            return new Parameters
            {
                PackageSet = @"..\ExtractPackages.txt",
                DeploymentFolder = @"..\"
            };
        }

        private static void WriteError(string msg)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }
    }
}
