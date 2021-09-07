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
using System.IO;
using System.Linq;

namespace Rhetos.Extensibility.Test
{
    [TestClass]
    public class PluginScannerTest
    {
        /// <summary>
        /// PluginsScanner uses <see cref="CsUtility.ReportTypeLoadException"/> to analyze and report type load exceptions,
        /// this feature helps detect and debug common issues with incompatible plugin versions or packages with incomplete dependencies.
        /// </summary>
        [TestMethod]
        public void AnalyzeAndReportTypeLoadException()
        {
            // The "TestReference" project is used because it has dependencies to libraries that are not available in the current project.
            string incompatibleAssemblyPath = FindIncompatibleAssemblyPath("Rhetos.Extensibility.TestReference");

            // Copying the "TestReference" assembly to local folder, to avoid automatic detection of dependency libraries that exist in its original location.
            // The goal is to try loading the test assembly without dependencies available.
            CopyAssemblyToLocalFolder(ref incompatibleAssemblyPath);

            // Searching for plugins in the "TestReference" assembly should fail because is references dependency that is not available.
            var pluginsScanner = new PluginScanner(new[] { incompatibleAssemblyPath }, new RhetosBuildEnvironment { CacheFolder = "." }, new ConsoleLogProvider(), new PluginScannerOptions());
            TestUtility.ShouldFail<FrameworkException>(
                () => pluginsScanner.FindPlugins(typeof(ICloneable)),
                "Please check if the assembly is missing or has a different version.",
                // The error shout report: (1) the assembly that causes the error, and (2) the missing assembly that is required for the first one to load.
                "'Rhetos.Extensibility.TestReference.dll' throws FileNotFoundException: Could not load file or assembly 'Microsoft.Extensions.Primitives, Version=5.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.");
        }

        private static string FindIncompatibleAssemblyPath(string testReferenceProject)
        {
            string startFolder = Directory.GetCurrentDirectory();
            var sourceFolder = new DirectoryInfo(startFolder);
            while (true)
            {
                if (Directory.Exists(Path.Combine(sourceFolder.FullName, testReferenceProject)))
                    break;
                if (sourceFolder.Parent == null)
                    throw new ArgumentException($"Invalid unit test setup: Cannot find test reference project '{testReferenceProject}'.");
                sourceFolder = sourceFolder.Parent;
            }

            string testReferenceAssemblyPath = Path.Combine(sourceFolder.FullName, testReferenceProject, Path.Combine("bin", "Debug", "net5.0", testReferenceProject + ".dll"));
            if (!File.Exists(testReferenceAssemblyPath))
                Assert.Fail($"Invalid unit test setup: Cannot find the test reference assembly '{testReferenceAssemblyPath}'. Make sure the build has passed successfully.");
            return testReferenceAssemblyPath;
        }

        private void CopyAssemblyToLocalFolder(ref string incompatibleAssemblyPath)
        {
            string localFolder = Path.GetDirectoryName(GetType().Assembly.Location);
            string newAssemblyPath = Path.Combine(localFolder, Path.GetFileName(incompatibleAssemblyPath));
            new FilesUtility(new ConsoleLogProvider()).SafeCopyFile(incompatibleAssemblyPath, newAssemblyPath, overwrite: true);
            incompatibleAssemblyPath = newAssemblyPath;
        }
    }
}
