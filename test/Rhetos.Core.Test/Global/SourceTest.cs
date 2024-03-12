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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Core.Test.Global
{
    [TestClass]
    public class SourceTest
    {
        [TestMethod]
        public void LineEndings()
        {
            var files = SourceUtility.GetSourceFiles(file => Path.GetExtension(file) is ".cs" or ".sql" or ".resx" or ".rhe");

            List<string> errors = files
                .AsParallel()
                .Select(filePath =>
                {
                    string content = File.ReadAllText(filePath);

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
                .AsEnumerable()
                .Where(fileAnalysis => fileAnalysis.eolKinds != @"\r\n" && fileAnalysis.eolKinds != "")
                .Select(fileAnalysis => $@"File line endings are '{fileAnalysis.eolKinds}' instead of '\r\n' (countN {fileAnalysis.countN}, countR {fileAnalysis.countR}, countRN {fileAnalysis.countRN}): {fileAnalysis.filePath}")
                .ToList();

            Console.WriteLine(string.Join(Environment.NewLine, errors));

            Assert.AreEqual("", string.Join(Environment.NewLine, errors));
        }

        [TestMethod]
        public void LicenseHeader()
        {
            var files = SourceUtility.GetSourceFiles(file => Path.GetExtension(file) is ".cs");

            var errors = files
                .AsParallel()
                .Select(filePath =>
                {
                    string beginning = ReadBeginning(filePath);
                    if (licenses.Any(license => beginning.StartsWith(license, StringComparison.Ordinal)))
                        return null;
                    else if (beginning.Contains("license", StringComparison.OrdinalIgnoreCase))
                        return new { filePath, error = "Has license, but not in expected format." };
                    else
                        return new { filePath, error = "No license found at the beginning of the file." };
                })
                .AsEnumerable()
                .Where(error => error != null)
                .ToList();

            Console.WriteLine(string.Join(Environment.NewLine, errors));
            Assert.AreEqual("", string.Join(Environment.NewLine, errors));
        }

        static readonly string[] licenses =
        [
            """
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
            """,
            """
            // Licensed to the .NET Foundation under one or more agreements.
            // The .NET Foundation licenses this file to you under the MIT license.
            """,
        ];

        private static string ReadBeginning(string filePath)
        {
            char[] buffer = new char[1000];
            using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
            int numberOfCharactersRead = reader.Read(buffer, 0, buffer.Length);
            return new string(buffer, 0, numberOfCharactersRead);
        }
    }
}
