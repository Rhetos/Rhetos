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

namespace Rhetos.TestCommon
{
    public static class SourceUtility
    {
        /// <summary>
        /// Returns all source files with the file path passing the filesFilter. For example:
        /// <code>
        /// SourceUtility.GetSourceFiles(file => Path.GetExtension(file) is ".cs" or ".sql")
        /// </code>
        /// </summary>
        /// <remarks>
        /// The root source folder is automatically detected by searching for <see cref="RootFolderContainsSubfolder"/> (configurable).
        /// </remarks>
        public static IReadOnlyCollection<string> GetSourceFiles(Func<string, bool> filesFilter)
        {
            var mainSubdirectories = new[] { "src", "test" };
            return GetSourceFiles(mainSubdirectories, filesFilter);
        }

        /// <summary>
        /// Returns all source files, withing the given subfolders, with the file path passing the filesFilter. For example:
        /// <code>
        /// SourceUtility.GetSourceFiles(new[] { "src", "test\\TestApp" }, file => Path.GetExtension(file) is ".cs" or ".sql")
        /// </code>
        /// </summary>
        /// <remarks>
        /// The root source folder is automatically detected by searching for <see cref="RootFolderContainsSubfolder"/> (configurable).
        /// </remarks>
        public static IReadOnlyCollection<string> GetSourceFiles(IEnumerable<string> subdirectories, Func<string, bool> filesFilter)
        {
            string root = FindRhetosProjectRootPath();
            var searchFolders = subdirectories.Select(dir => Path.GetFullPath(Path.Combine(root, dir))).ToList();
            var skipFoldersSuffix = notSourceFolders.Select(dir => Path.DirectorySeparatorChar + dir).ToArray();
            bool directoriesFilter(string dir) => !skipFoldersSuffix.Any(skipFolder => dir.EndsWith(skipFolder, StringComparison.OrdinalIgnoreCase));

            return GetFiles(searchFolders, directoriesFilter, filesFilter).ToList();
        }

        private static readonly string[] notSourceFolders = ["bin", "obj", "TestResults"];

        /// <summary>
        /// Used for searching for root source folder, from a test project, because the tests can be run in different working folders depending on how they are executed.
        /// Override this value to customize for a different project.
        /// </summary>
        public static string RootFolderContainsSubfolder { get; set; } = Path.Combine("src", "Rhetos.Core");

        private static string FindRhetosProjectRootPath()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                dir = dir.Parent;
                if (Directory.Exists(Path.Combine(dir.FullName, RootFolderContainsSubfolder)))
                    return dir.FullName;
            }
            throw new ArgumentException($"Cannot locate the Rhetos project root path, starting from '{Directory.GetCurrentDirectory()}'.");
        }

        private static IEnumerable<string> GetDirectories(IEnumerable<string> directories, Func<string, bool> filter)
        {
            var newDirectories = directories.SelectMany(Directory.GetDirectories).Where(filter).ToList();

            if (newDirectories.Count == 0)
                return [];

            return newDirectories.Concat(GetDirectories(newDirectories, filter));
        }

        private static IEnumerable<string> GetFiles(IEnumerable<string> directories, Func<string, bool> directoriesFilter, Func<string, bool> filesFilter)
        {
            return GetDirectories(directories, directoriesFilter)
                .SelectMany(Directory.GetFiles)
                .Where(filesFilter);
        }
    }
}