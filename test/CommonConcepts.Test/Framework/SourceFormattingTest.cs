﻿/*
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class SourceFormattingTest
    {
        private static string FindRhetosProjectRootPath()
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

        [TestMethod]
        public void LineEndings()
        {
            var files = Directory.GetFiles(FindRhetosProjectRootPath(), "*.cs", SearchOption.AllDirectories);

            string binPattern = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
            string objPattern = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;

            List<string> errors = files
                .Where(filePath => !filePath.Contains(binPattern) && !filePath.Contains(objPattern))
                .Select(filePath =>
                {
                    string content =  File.ReadAllText(filePath);

                    int countN = 0;
                    int countR = 0;
                    int countRN = 0;

                    int i = 0;
                    while (i < content.Length)
                    {
                        char c = content[i];
                        char next = i + 1 < content.Length ? content[i + 1] : default;

                        if (c == '\r' && next == '\n')
                        {
                            countRN++;
                            i += 2;
                        }
                        else if (c == '\n')
                        {
                            countN++;
                            i++;
                        }
                        else if (c == '\r')
                        {
                            countR++;
                            i++;
                        }
                        else
                            i++;
                    }

                    string eolKinds = string.Join(", ",
                        new[] { countN > 0 ? @"\n" : null, countR > 0 ? @"\r" : null, countRN > 0 ? @"\r\n" : null }
                            .Where(x => x != null));

                    return new { filePath, countN, countR, countRN, eolKinds };
                })
                .Where(fileAnalysis => fileAnalysis.eolKinds != @"\r\n")
                .Select(fileAnalysis => $@"File line endings are '{fileAnalysis.eolKinds}' instead of '\r\n' (countN {fileAnalysis.countN}, countR {fileAnalysis.countR}, countRN {fileAnalysis.countRN}): {fileAnalysis.filePath}")
                .ToList();
            Console.WriteLine(errors);

            Assert.AreEqual("", string.Join(Environment.NewLine, errors));
        }
    }
}
