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

using Autofac.Core;
using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class SourceTest
    {
        [TestMethod]
        public void LineEndings()
        {
            var files = SourceUtility.GetSourceFiles(file => Path.GetExtension(file) is ".cs" or ".sql" or ".resx" or ".rhe");

            List<string> errors = files
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
                .Where(fileAnalysis => fileAnalysis.eolKinds != @"\r\n" && fileAnalysis.eolKinds != "")
                .Select(fileAnalysis => $@"File line endings are '{fileAnalysis.eolKinds}' instead of '\r\n' (countN {fileAnalysis.countN}, countR {fileAnalysis.countR}, countRN {fileAnalysis.countRN}): {fileAnalysis.filePath}")
                .ToList();
            Console.WriteLine(errors);

            Assert.AreEqual("", string.Join(Environment.NewLine, errors));
        }

        [TestMethod]
        public void SqlRepositoryUsage()
        {
            var resxFiles = SourceUtility.GetSourceFiles(
                new[] { Path.Combine("src", "Rhetos.MsSql") },
                file => Path.GetExtension(file) is ".resx");
            var resxKeys = resxFiles.SelectMany(file =>
            {
                var xml = XDocument.Load(file);
                return xml.Root.Elements("data").Attributes("name").Select(a => a.Value);
            });

            var sourceFiles = SourceUtility.GetSourceFiles(new[] { "src" }, file => Path.GetExtension(file) is ".cs" or ".rhe");

            var unusedKeys = resxKeys.ToHashSet();
            var usedKeys = new HashSet<string>();

            string iSqlResourcesMethodCall = @"(sql|resources)\.(TryGet|Get|Format)\(";
            string iSqlResourcesParameter = @"\bsqlResource:";
            string iSqlResourcesOptions = $"(({iSqlResourcesMethodCall})|({iSqlResourcesParameter}))";
            var iSqlResourcesUsageRegex = new Regex(iSqlResourcesOptions + @"\s*""(?<key>.+?)""", RegexOptions.IgnoreCase);

            foreach (var sourceFile in sourceFiles)
            {
                string source = File.ReadAllText(sourceFile);

                var foundUnusedKeys = unusedKeys.Where(key => source.Contains(key, StringComparison.Ordinal)).ToList();
                unusedKeys.ExceptWith(foundUnusedKeys);

                var foundAllResourcesKeyUsage = iSqlResourcesUsageRegex.Matches(source).Cast<Match>().Select(match => match.Groups["key"].Value).ToList();
                usedKeys.UnionWith(foundAllResourcesKeyUsage);

                var foundResxKeysOtherThanResourcesKeyUsage = foundUnusedKeys.Except(foundAllResourcesKeyUsage).ToList();
                if (foundResxKeysOtherThanResourcesKeyUsage.Any())
                {
                    Console.WriteLine(TestUtility.Dump(foundResxKeysOtherThanResourcesKeyUsage));
                    Assert.Fail($"Found usage of key '{foundResxKeysOtherThanResourcesKeyUsage.First()}' from .resx file, without detecting the use of ISqlResources, in file '{sourceFile}'.");
                }
            }

            var usedUndefinedKeys = usedKeys.Except(resxKeys).ToList();

            // The logging concepts dynamically generate resource keys based on property type.
            unusedKeys.RemoveWhere(key => key.StartsWith("PropertyLoggingDefinition_TextValue_"));

            string report = $"unusedKeys={unusedKeys.Count}, usedUndefinedKeys={usedUndefinedKeys.Count}"
                + Environment.NewLine + $"unusedKeys: {TestUtility.Dump(unusedKeys, key => Environment.NewLine + "    " + key)}"
                + Environment.NewLine + $"usedUndefinedKeys: {TestUtility.Dump(usedUndefinedKeys, key => Environment.NewLine + "    " + key)}";
            string expectedReport = $"unusedKeys=0, usedUndefinedKeys=0"
                + Environment.NewLine + $"unusedKeys: "
                + Environment.NewLine + $"usedUndefinedKeys: ";
            Assert.AreEqual(expectedReport, report);
        }
    }
}
