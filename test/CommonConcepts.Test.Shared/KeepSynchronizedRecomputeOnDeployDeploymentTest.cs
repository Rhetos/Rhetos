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
#if RHETOS_MSSQL
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class KeepSynchronizedRecomputeOnDeployDeploymentTest
    {
        private const string FixtureName = "KeepSynchronizedDeploymentFixture";
        private const string RecomputeMessage = "Recomputing DeploymentTest.Target from DeploymentTest.Source.";
        private const string MetadataQuery = "SELECT Context FROM Common.KeepSynchronizedMetadata WHERE Source = 'DeploymentTest.Source' AND Target = 'DeploymentTest.Target'";
        private const string SourceDefinitionQuery = "SELECT OBJECT_DEFINITION(OBJECT_ID('DeploymentTest.Source'))";
        private const string AddedColumnQuery = "SELECT COL_LENGTH('DeploymentTest.Target', 'Added')";

        [TestMethod]
        [TestCategory("Deployment")]
        public async Task Adding_a_target_property_recomputes_existing_records_on_dbupdate()
        {
            string packageVersion = typeof(KeepSynchronizedRecomputeOnDeployCodeGenerator).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];
            string packageFeed = FindPackageFeed();
            Assert.IsTrue(File.Exists(Path.Combine(packageFeed, $"Rhetos.CommonConcepts.{packageVersion}.nupkg")),
                "Build the local NuGet packages before running deployment tests. The fixture must use the same Rhetos version as the test app.");

            string databaseName = "RhetosKeepSyncDeployment_" + Guid.NewGuid().ToString("N");
            string fixtureFolder = Path.Combine(Path.GetTempPath(), databaseName);
            SqlConnectionStringBuilder databaseConnection;
            using (var scope = TestScope.Create())
                databaseConnection = new SqlConnectionStringBuilder(scope.Resolve<ConnectionString>().ToString())
                {
                    InitialCatalog = databaseName,
                    AttachDBFilename = "",
                    Pooling = false,
                    Enlist = false
                };

            string tempdbConnection = new SqlConnectionStringBuilder(databaseConnection.ConnectionString)
            {
                InitialCatalog = "tempdb"
            }.ConnectionString;

            bool databaseCreated = false;
            bool testFailed = true;
            Directory.CreateDirectory(fixtureFolder);
            try
            {
                WriteFixture(fixtureFolder, packageVersion, databaseConnection.ConnectionString);
                await RunDotnet(fixtureFolder, "restore", FixtureName + ".csproj",
                    "--source", packageFeed, "--source", "https://api.nuget.org/v3/index.json", "--ignore-failed-sources");
                await BuildFixture(fixtureFolder);

                string fixtureBinary = Path.Combine(fixtureFolder, "bin", "Debug", "net8.0", "Rhetos.CommonConcepts.dll");
                string binaryHash = FileHash(fixtureBinary);
                Assert.AreEqual(FileHash(typeof(KeepSynchronizedRecomputeOnDeployCodeGenerator).Assembly.Location), binaryHash,
                    "The fixture must use the same CommonConcepts binary as the test app.");

                // Only this newly created database is modified. No deployment uses the shared test database.
                ExecuteSql(tempdbConnection, $"CREATE DATABASE [{databaseName}]");
                databaseCreated = true;
                await DeployFixture(fixtureFolder);

                // Version 1 deploys the source (with both values) and populates the target's Name property.
                AssertTargetRecord(databaseConnection.ConnectionString, hasAddedProperty: false);
                Assert.IsNull(ReadScalar(databaseConnection.ConnectionString, AddedColumnQuery));
                string oldContext = ReadText(databaseConnection.ConnectionString, MetadataQuery);
                StringAssert.Contains(oldContext, "TargetComputedProperties: Name=Name");
                string sourceDefinition = ReadText(databaseConnection.ConnectionString, SourceDefinitionQuery);

                // Version 2 changes only the target mapping. Restore is not repeated, so packages cannot change.
                File.WriteAllText(Path.Combine(fixtureFolder, "Fixture.rhe"),
                    FixtureDsl.Replace(AddedMappingMarker, "ShortString Added { ComputedFrom DeploymentTest.Source.Added; }", StringComparison.Ordinal));
                await BuildFixture(fixtureFolder);
                Assert.AreEqual(binaryHash, FileHash(fixtureBinary), "Both deployments must use identical CommonConcepts binaries.");
                Assert.IsNull(ReadScalar(databaseConnection.ConnectionString, AddedColumnQuery), "Building must not deploy the new column.");
                Assert.AreEqual(oldContext, ReadText(databaseConnection.ConnectionString, MetadataQuery));

                string deploymentOutput = await DeployFixture(fixtureFolder);
                StringAssert.Contains(deploymentOutput, RecomputeMessage);
                AssertTargetRecord(databaseConnection.ConnectionString, hasAddedProperty: true);
                Assert.AreEqual(sourceDefinition, ReadText(databaseConnection.ConnectionString, SourceDefinitionQuery),
                    "The source SQL view must remain unchanged between deployments.");
                string newContext = ReadText(databaseConnection.ConnectionString, MetadataQuery);
                Assert.AreNotEqual(oldContext, newContext);
                StringAssert.Contains(newContext, "TargetComputedProperties: Added=Added, Name=Name");
                Assert.AreEqual(SourceContext(oldContext), SourceContext(newContext), "Only the target mapping should change the context.");

                // A third, unchanged deployment must not recompute this target again.
                string unchangedDeploymentOutput = await DeployFixture(fixtureFolder);
                Assert.IsFalse(unchangedDeploymentOutput.Contains(RecomputeMessage, StringComparison.Ordinal),
                    "An unchanged deployment must not recompute the target.");
                Assert.AreEqual(newContext, ReadText(databaseConnection.ConnectionString, MetadataQuery));
                AssertTargetRecord(databaseConnection.ConnectionString, hasAddedProperty: true);
                testFailed = false;
            }
            finally
            {
                try
                {
                    if (databaseCreated)
                        ExecuteSql(tempdbConnection, $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]");
                }
                catch (Exception exception) when (testFailed)
                {
                    // Preserve the original failure, but report the owned database that could not be cleaned up.
                    Console.WriteLine($"Could not drop test database '{databaseName}': {exception}");
                }
                finally
                {
                    try
                    {
                        // Rhetos marks generated sources read-only; use its cleanup helper to clear those attributes.
                        new FilesUtility(new ConsoleLogProvider()).SafeDeleteDirectory(fixtureFolder);
                    }
                    catch (Exception exception) when (testFailed)
                    {
                        Console.WriteLine($"Could not delete fixture folder '{fixtureFolder}': {exception}");
                    }
                }
            }
        }

        private static string FindPackageFeed()
        {
            string project = SourceUtility.GetSourceFiles(new[] { "src" },
                path => Path.GetFileName(path) == "Rhetos.CommonConcepts.csproj").Single();
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project), "..", "..", "dist"));
        }

        private static void WriteFixture(string folder, string packageVersion, string connectionString)
        {
            string[] packages = ["Rhetos", "Rhetos.CommonConcepts", "Rhetos.MsSql", "Rhetos.MSBuild", "Rhetos.Host"];
            var project = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("OutputType", "Exe"),
                    new XElement("TargetFramework", "net8.0"),
                    new XElement("RhetosDeploy", "false")),
                new XElement("ItemGroup", packages.Select(package =>
                    new XElement("PackageReference", new XAttribute("Include", package), new XAttribute("Version", $"[{packageVersion}]")))),
                new XElement("ItemGroup", new XElement("Content", new XAttribute("Include", "local.settings.json"),
                    new XElement("CopyToOutputDirectory", "PreserveNewest"))));
            project.Save(Path.Combine(folder, FixtureName + ".csproj"));
            File.WriteAllText(Path.Combine(folder, "Program.cs"), FixtureProgram);
            File.WriteAllText(Path.Combine(folder, "Fixture.rhe"), FixtureDsl.Replace(AddedMappingMarker, "", StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(folder, "local.settings.json"), JsonSerializer.Serialize(new
            {
                ConnectionStrings = new { RhetosConnectionString = connectionString },
                Rhetos = new { DbUpdate = new { SkipRecompute = false } }
            }));
        }

        private static Task<string> BuildFixture(string folder) => RunDotnet(folder,
            "build", FixtureName + ".csproj", "--configuration", "Debug", "--no-restore", "/t:Rebuild", "/p:RhetosDeploy=false");

        private static Task<string> DeployFixture(string folder)
        {
            string outputFolder = Path.Combine(folder, "bin", "Debug", "net8.0");
            return RunDotnet(folder, Path.Combine(outputFolder, "rhetos.dll"), "dbupdate", Path.Combine(outputFolder, FixtureName + ".dll"));
        }

        private static async Task<string> RunDotnet(string folder, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = folder,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);
            startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            bool timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            string output = await stdout + Environment.NewLine + await stderr;
            Console.WriteLine($"dotnet {string.Join(' ', arguments)}{Environment.NewLine}{output}");
            Assert.IsFalse(timedOut, "The fixture command exceeded its five-minute timeout.");
            Assert.AreEqual(0, process.ExitCode, "The fixture command failed. See its captured output.");
            return output;
        }

        private static void ExecuteSql(string connectionString, string sql)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
            command.ExecuteNonQuery();
        }

        private static object ReadScalar(string connectionString, string sql)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            object value = command.ExecuteScalar();
            return value == DBNull.Value ? null : value;
        }

        private static string ReadText(string connectionString, string sql)
        {
            object value = ReadScalar(connectionString, sql);
            Assert.IsInstanceOfType<string>(value, "Expected a database value for: " + sql);
            return (string)value;
        }

        private static void AssertTargetRecord(string connectionString, bool hasAddedProperty)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            string sql = hasAddedProperty ? "SELECT ID, Name, Added FROM DeploymentTest.Target" : "SELECT ID, Name FROM DeploymentTest.Target";
            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read(), "The target record from the first deployment must still exist.");
            Assert.AreEqual(new Guid("1B12673F-D9E9-4F73-8C42-44DD630752B5"), reader.GetGuid(0));
            Assert.AreEqual("original value", reader.GetString(1));
            if (hasAddedProperty)
            {
                Assert.IsFalse(reader.IsDBNull(2), "dbupdate must populate the newly added computed property on existing records.");
                Assert.AreEqual("newly mapped value", reader.GetString(2));
            }
            Assert.IsFalse(reader.Read(), "Deployment must not add duplicate target records.");
        }

        private static string FileHash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        private static string SourceContext(string context) => context.Substring(0, context.IndexOf(" | TargetComputedProperties: ", StringComparison.Ordinal));

        private const string AddedMappingMarker = "// ADD_TARGET_MAPPING";

        private const string FixtureDsl = """
            Module DeploymentTest
            {
                SqlQueryable Source
                    "SELECT ID = CONVERT(UNIQUEIDENTIFIER, '1B12673F-D9E9-4F73-8C42-44DD630752B5'),
                        Name = CONVERT(NVARCHAR(256), 'original value'),
                        Added = CONVERT(NVARCHAR(256), 'newly mapped value')"
                {
                    ShortString Name;
                    ShortString Added;
                }

                Entity Target
                {
                    ShortString Name { ComputedFrom DeploymentTest.Source.Name; }
                    ComputedFrom DeploymentTest.Source { KeepSynchronized; }
                    // ADD_TARGET_MAPPING
                }
            }
            """;

        private const string FixtureProgram = """
            using Autofac;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Rhetos;
            using Rhetos.Security;
            using Rhetos.Utilities;
            using System;

            public static class Program
            {
                public static void Main() { }

                public static IHostBuilder CreateHostBuilder(string[] args)
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services => services.AddRhetosHost((IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder) =>
                        rhetosHostBuilder
                            .ConfigureRhetosAppDefaults()
                            .ConfigureConfiguration(configuration => configuration.AddJsonFile("local.settings.json"))
                            .ConfigureContainer(container => container.RegisterType<ProcessUserInfo>().As<IUserInfo>())));
                    return builder;
                }
            }
            """;
    }
}
#endif
