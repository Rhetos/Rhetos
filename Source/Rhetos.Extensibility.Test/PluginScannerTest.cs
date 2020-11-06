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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rhetos.Extensibility.Test
{
    [TestClass]
    public class PluginScannerTest
    {
        /// <summary>
        /// PluginsScanner uses <see cref="CsUtility.ReportTypeLoadException"/> to analyze and report type load exceptions,
        /// a common issue when using incompatible plugin versions.
        /// </summary>
        [TestMethod]
        public void AnalyzeAndReportTypeLoadException()
        {
            var incompatibleAssemblies = new[] { GetIncompatibleAssemblyPath() };
            var pluginsScanner = new PluginScanner(incompatibleAssemblies, ".", new ConsoleLogProvider(), new PluginScannerOptions());
            TestUtility.ShouldFail<FrameworkException>(
                () => pluginsScanner.FindPlugins(typeof(IGenerator)),
                "Please check if the assembly is missing or has a different version.",
                "'Rhetos.RestGenerator.dll' throws FileNotFoundException: Could not load file or assembly 'Rhetos.Compiler.Interfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.");
        }

        private static string GetIncompatibleAssemblyPath()
        {
            string startingFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string oldAssemblyPathInRepository = @"packages\Rhetos.RestGenerator.2.1.0\lib\net451\Rhetos.RestGenerator.dll";
            var testFolder = new DirectoryInfo(startingFolder);

            string oldAssemblyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".nuget\packages\rhetos.restgenerator\2.1.0\lib\net451\Rhetos.RestGenerator.dll");
            if (!File.Exists(oldAssemblyPath))
                Assert.Fail($"Invalid unit test setup: Cannot find '{oldAssemblyPathInRepository}'" +
                    $" starting from '{startingFolder}' or any parent folder. See the test output for more details.");
            return oldAssemblyPath;
        }
    }
}
