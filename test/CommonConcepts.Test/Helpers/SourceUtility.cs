using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace CommonConcepts.Test.Helpers
{
    internal static class SourceUtility
    {
        public static ICollection<string> GetSourceFiles(Func<string, bool> filesFilter)
        {
            var mainSubdirectories = new[] { "src", "test" };
            return GetSourceFiles(mainSubdirectories, filesFilter);
        }

        public static ICollection<string> GetSourceFiles(IEnumerable<string> subdirectories, Func<string, bool> filesFilter)
        {
            string root = FindRhetosProjectRootPath();
            var searchFolders = subdirectories.Select(dir => Path.Combine(root, dir)).ToList();
            var skipFoldersSuffix = new[] { "bin", "obj", "TestResults" }.Select(dir => Path.DirectorySeparatorChar + dir).ToArray();
            Func<string, bool> directoriesFilter = dir => !skipFoldersSuffix.Any(skipFolder => dir.EndsWith(skipFolder, StringComparison.OrdinalIgnoreCase));

            return GetFiles(searchFolders, directoriesFilter, filesFilter).ToList();
        }

        public static string FindRhetosProjectRootPath()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                dir = dir.Parent;
                if (Directory.Exists(Path.Combine(dir.FullName, "src", "Rhetos.Core")))
                    return dir.FullName;
            }
            throw new ArgumentException($"Cannot locate the Rhetos project root path, starting from '{Directory.GetCurrentDirectory()}'.");
        }

        public static IEnumerable<string> GetDirectories(IEnumerable<string> directories, Func<string, bool> filter)
        {
            var newDirectories = directories.SelectMany(Directory.GetDirectories).Where(filter).ToList();

            if (newDirectories.Count == 0)
                return Array.Empty<string>();

            return newDirectories.Concat(GetDirectories(newDirectories, filter));
        }

        public static IEnumerable<string> GetFiles(IEnumerable<string> directories, Func<string, bool> directoriesFilter, Func<string, bool> filesFilter)
        {
            return GetDirectories(directories, directoriesFilter)
                .SelectMany(Directory.GetFiles)
                .Where(filesFilter);
        }
    }
}