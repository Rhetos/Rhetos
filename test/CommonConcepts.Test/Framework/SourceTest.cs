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
using Rhetos;
using Rhetos.TestCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class SourceTest
    {
        [TestMethod]
        public void SqlResourceUsage()
        {
            //=============================================================
            // List SQL snippets that are available in assembly resources:

            using var scope = TestScope.Create();
            Assert.IsNotNull(scope.Resolve<ISqlResources>());
            Assert.IsNotNull(scope.Resolve<IEnumerable<Autofac.Module>>()); // Loading all referenced assemblies that might contain resources files. Otherwise CurrentDomain.GetAssemblies() might not return those assemblies.

            var assemblyResources = AppDomain.CurrentDomain.GetAssemblies() // GetAssemblies() returns only those referenced assemblies that have been loaded (used) before calling this method.
                .Where(a => a.FullName.StartsWith("Rhetos"))
                .SelectMany(a => a.GetManifestResourceNames().Select(r => new { a, r }))
                .Where(ar => ar.r.EndsWith(".resources"))
                .ToList();
            if (assemblyResources.Count < 2)
                throw new ArgumentException($"Expecting multiple assemblyResources. Count={assemblyResources.Count}.");

            var resourceKeys = new List<string>();
            foreach (var ar in assemblyResources)
            {
                Console.WriteLine($"Loading {ar.a.GetName().Name}: {ar.r}");
                var resourceManager = new ResourceManager(Path.GetFileNameWithoutExtension(ar.r), ar.a);
                ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
                resourceKeys.AddRange(resourceSet.Cast<DictionaryEntry>().Select(e => e.Key.ToString()));
            }

            //=============================================================
            // List SQL snippets that are used in source files:

            var sourceFiles = SourceUtility.GetSourceFiles(new[] { "src" }, file => Path.GetExtension(file) is ".cs" or ".rhe");
            if (sourceFiles.Count < 50)
                throw new ArgumentException($"Missing some sourceFiles. Count={sourceFiles.Count}.");

            var unusedKeys = resourceKeys.ToHashSet();
            var usedKeys = new HashSet<string>();

            string iSqlResourcesMethodCall = @"(sql|resources)\.(Get|TryGet|Format|TryFormat)\(";
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
                if (foundResxKeysOtherThanResourcesKeyUsage.Count != 0)
                {
                    Console.WriteLine(TestUtility.Dump(foundResxKeysOtherThanResourcesKeyUsage));
                    Assert.Fail($"Found usage of key '{foundResxKeysOtherThanResourcesKeyUsage.First()}' from .resx file, without detecting the use of ISqlResources, in file '{sourceFile}'.");
                }
            }

            //=============================================================
            // Test for snippets that are provided but not used, or used but not provided:

            // Adding keys that are dynamically used in source:
            foreach (var propertyType in new[] { "Binary", "Bool", "Date", "DateTime", "Decimal", "Integer", "LongString", "Money", "Reference", "ShortString" })
                unusedKeys.Remove($"StorageMappingDbType_{propertyType}");

            var usedUndefinedKeys = usedKeys.Except(resourceKeys).ToList();

#if RHETOS_EF6 || RHETOS_MSSQL
            usedUndefinedKeys.Remove("SqlIndexClusteredDatabaseDefinition_Create"); // MS SQL does not have a separate command for clustered index.
            usedUndefinedKeys.Remove("SqlIndexClusteredDatabaseDefinition_Remove");
            usedUndefinedKeys.Remove("PropertyLoggingDefinition_GenericPropertyDeletedLogging"); // MS SQL does not use LogPropertyDeleted tag in the logging trigger.
#endif

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
