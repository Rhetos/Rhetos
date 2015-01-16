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

using Autofac.Features.Indexed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsDemo
    {
        [TestMethod]
        public void SimpleRowPermissions()
        {
            SetBasicAccessPermissions<DemoRowPermissions1.Document>(); // Not related to row permissions.

            // Insert the test data (server code bypasses row permissions):

            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var repository = container.Resolve<Common.DomRepository>();
                var context = container.Resolve<Common.ExecutionContext>();
                repository.DemoRowPermissions1.Document.Delete(repository.DemoRowPermissions1.Document.All());
                repository.DemoRowPermissions1.Employee.Delete(repository.DemoRowPermissions1.Employee.All());
                repository.DemoRowPermissions1.Division.Delete(repository.DemoRowPermissions1.Division.All());

                var div1 = new DemoRowPermissions1.Division { Name = "div1" };
                var div2 = new DemoRowPermissions1.Division { Name = "div2" };
                repository.DemoRowPermissions1.Division.Insert(new[] { div1, div2 });

                // The current user:
                var emp1 = new DemoRowPermissions1.Employee
                {
                    UserName = context.UserInfo.UserName,
                    Division = div1
                };
                repository.DemoRowPermissions1.Employee.Insert(new[] { emp1 });

                // The user can access doc1, because it's in the same division:
                var doc1 = new DemoRowPermissions1.Document { Title = "doc1", Division = div1 };
                // The user cannot access doc2:
                var doc2 = new DemoRowPermissions1.Document { Title = "doc2", Division = div2 };
                repository.DemoRowPermissions1.Document.Insert(new[] { doc1, doc2 });
            }

            // Simulate client request: Reading all documents (access denied)

            using (var container = new RhetosTestContainer())
            {
                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new ReadCommandInfo
                {
                    DataSource = typeof(DemoRowPermissions1.Document).FullName,
                    ReadRecords = true
                };
                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.IsTrue(report.Contains("Insufficient permissions"));
            }

            // Simulate client request: Reading the user's documents

            using (var container = new RhetosTestContainer())
            {
                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new ReadCommandInfo
                {
                    DataSource = typeof(DemoRowPermissions1.Document).FullName,
                    ReadRecords = true,
                    Filters = new[] { new FilterCriteria(typeof(Common.RowPermissionsReadItems)) }
                };
                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.AreEqual("doc1", report);
            }
        }

        [TestMethod]
        public void ComplexRowPermissions()
        {
            SetBasicAccessPermissions<DemoRowPermissions2.Document>(); // Not related to row permissions.

            // Insert the test data (server code bypasses row permissions):

            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var repository = container.Resolve<Common.DomRepository>();
                var context = container.Resolve<Common.ExecutionContext>();
                repository.DemoRowPermissions2.DocumentApproval.Delete(repository.DemoRowPermissions2.DocumentApproval.All());
                repository.DemoRowPermissions2.DocumentComment.Delete(repository.DemoRowPermissions2.DocumentComment.All());
                repository.DemoRowPermissions2.Document.Delete(repository.DemoRowPermissions2.Document.All());
                repository.DemoRowPermissions2.RegionSupervisor.Delete(repository.DemoRowPermissions2.RegionSupervisor.All());
                repository.DemoRowPermissions2.Employee.Delete(repository.DemoRowPermissions2.Employee.All());
                repository.DemoRowPermissions2.Division.Delete(repository.DemoRowPermissions2.Division.All());
                repository.DemoRowPermissions2.Region.Delete(repository.DemoRowPermissions2.Region.All());

                var reg3 = new DemoRowPermissions2.Region { Name = "reg3" };
                repository.DemoRowPermissions2.Region.Insert(new[] { reg3 });

                var div1 = new DemoRowPermissions2.Division { Name = "div1" };
                var div2 = new DemoRowPermissions2.Division { Name = "div2" };
                var div3 = new DemoRowPermissions2.Division { Name = "div3", Region = reg3 };
                repository.DemoRowPermissions2.Division.Insert(new[] { div1, div2, div3 });

                // The current user:
                var emp1 = new DemoRowPermissions2.Employee
                {
                    UserName = context.UserInfo.UserName,
                    Division = div1
                };
                repository.DemoRowPermissions2.Employee.Insert(new[] { emp1 });

                var sup3 = new DemoRowPermissions2.RegionSupervisor
                {
                    Employee = emp1,
                    Region = reg3
                };
                repository.DemoRowPermissions2.RegionSupervisor.Insert(new[] { sup3 });

                // The user can access doc1, because it's in the same division:
                var doc1 = new DemoRowPermissions2.Document { Title = "doc1", Division = div1 };
                // The user cannot access doc2:
                var doc2 = new DemoRowPermissions2.Document { Title = "doc2", Division = div2 };
                // The user can access doc3, because it's in the region he supervises:
                var doc3 = new DemoRowPermissions2.Document { Title = "doc3", Division = div3 };
                // The user can access doc4 (same division), but cannot edit it (previous year):
                var doc4 = new DemoRowPermissions2.Document { Title = "doc4", Division = div1, Created = DateTime.Now.AddYears(-1) };
                repository.DemoRowPermissions2.Document.Insert(new[] { doc1, doc2, doc3, doc4 });
            }

            // Simulate client request: Reading all documents (access denied)

            using (var container = new RhetosTestContainer())
            {
                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new ReadCommandInfo
                {
                    DataSource = typeof(DemoRowPermissions2.Document).FullName,
                    ReadRecords = true
                };
                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.IsTrue(report.Contains("Insufficient permissions"));
            }

            // Simulate client request: Reading the user's documents

            using (var container = new RhetosTestContainer())
            {
                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new ReadCommandInfo
                {
                    DataSource = typeof(DemoRowPermissions2.Document).FullName,
                    ReadRecords = true,
                    Filters = new[] { new FilterCriteria(typeof(Common.RowPermissionsReadItems)) }
                };
                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.AreEqual("doc1, doc3, doc4", report);
            }

            // Simulate client request: Edit doc1 (ok)

            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var doc1 = repository.DemoRowPermissions2.Document.Query().Where(d => d.Title == "doc1").Single();
                doc1.Title += "x";

                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new SaveEntityCommandInfo
                {
                    Entity = typeof(DemoRowPermissions2.Document).FullName,
                    DataToUpdate = new[] { doc1 }
                };
                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.AreEqual("Comand executed", report);

                var documents = repository.DemoRowPermissions2.Document.Query().Select(d => d.Title).OrderBy(t => t);
                Assert.AreEqual("doc1x, doc2, doc3, doc4", string.Join(", ", documents));
            }

            // Simulate client request: Edit doc4 (acces denied)

            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var doc4 = repository.DemoRowPermissions2.Document.Query().Where(d => d.Title == "doc4").Single();
                doc4.Title += "x";

                var processingEngine = container.Resolve<IProcessingEngine>();
                var serverCommand = new SaveEntityCommandInfo
                {
                    Entity = typeof(DemoRowPermissions2.Document).FullName,
                    DataToUpdate = new[] { doc4 }
                };

                var serverResponse = processingEngine.Execute(new[] { serverCommand });
                var report = GenerateReport(serverResponse);
                Console.WriteLine("Server response: " + report);
                Assert.IsTrue(report.Contains("Insufficient permissions"));
            }
        }

        [TestMethod]
        public void InheritingRowPermissions()
        {
            SetBasicAccessPermissions<DemoRowPermissions2.Document>(); // Not related to row permissions.

            // Insert the test data (server code bypasses row permissions):

            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var repository = container.Resolve<Common.DomRepository>();
                var context = container.Resolve<Common.ExecutionContext>();
                repository.DemoRowPermissions2.DocumentApproval.Delete(repository.DemoRowPermissions2.DocumentApproval.All());
                repository.DemoRowPermissions2.DocumentComment.Delete(repository.DemoRowPermissions2.DocumentComment.All());
                repository.DemoRowPermissions2.Document.Delete(repository.DemoRowPermissions2.Document.All());
                repository.DemoRowPermissions2.RegionSupervisor.Delete(repository.DemoRowPermissions2.RegionSupervisor.All());
                repository.DemoRowPermissions2.Employee.Delete(repository.DemoRowPermissions2.Employee.All());
                repository.DemoRowPermissions2.Division.Delete(repository.DemoRowPermissions2.Division.All());
                repository.DemoRowPermissions2.Region.Delete(repository.DemoRowPermissions2.Region.All());

                var reg3 = new DemoRowPermissions2.Region { Name = "reg3" };
                repository.DemoRowPermissions2.Region.Insert(new[] { reg3 });

                var div1 = new DemoRowPermissions2.Division { Name = "div1" };
                var div2 = new DemoRowPermissions2.Division { Name = "div2" };
                var div3 = new DemoRowPermissions2.Division { Name = "div3", Region = reg3 };
                repository.DemoRowPermissions2.Division.Insert(new[] { div1, div2, div3 });

                // The current user:
                var emp1 = new DemoRowPermissions2.Employee
                {
                    UserName = context.UserInfo.UserName,
                    Division = div1
                };
                var emp2 = new DemoRowPermissions2.Employee
                {
                    UserName = "emp2"
                };
                repository.DemoRowPermissions2.Employee.Insert(new[] { emp1, emp2 });

                var sup3 = new DemoRowPermissions2.RegionSupervisor
                {
                    Employee = emp1,
                    Region = reg3
                };
                repository.DemoRowPermissions2.RegionSupervisor.Insert(new[] { sup3 });

                // The current user can access doc1, because it's in the same division:
                var doc1 = new DemoRowPermissions2.Document { Title = "doc1", Division = div1 };
                // The current user cannot access doc2:
                var doc2 = new DemoRowPermissions2.Document { Title = "doc2", Division = div2 };
                // The current user can access doc3, because it's in the region he supervises:
                var doc3 = new DemoRowPermissions2.Document { Title = "doc3", Division = div3 };
                repository.DemoRowPermissions2.Document.Insert(new[] { doc1, doc2, doc3 });

                // The current user can access com1, because it is related to his document:
                var com1 = new DemoRowPermissions2.DocumentComment { Document = doc1, Comment = "com1" };
                // The current user cannot access com2:
                var com2 = new DemoRowPermissions2.DocumentComment { Document = doc2, Comment = "com2" };
                repository.DemoRowPermissions2.DocumentComment.Insert(new[] { com1, com2 });

                // The current user can access app1, because it is related to his document:
                var app1 = new DemoRowPermissions2.DocumentApproval { ID = doc1.ID, ApprovedBy = emp1, Note = "app1" };
                // The current user cannot access app2:
                var app2 = new DemoRowPermissions2.DocumentApproval { ID = doc2.ID, ApprovedBy = emp1, Note = "app2" };
                // The current user can read app3, but cannot write it, because it is approved by a different user:
                var app3 = new DemoRowPermissions2.DocumentApproval { ID = doc3.ID, ApprovedBy = emp2, Note = "app3" };
                repository.DemoRowPermissions2.DocumentApproval.Insert(new[] { app1, app2, app3 });
            }

            // Test the current user's row permissions:
            // The test will not execute client requests, but simply directly check the row permissions filters.

            using (var container = new RhetosTestContainer())
            {
                var allowedReadBrowse =
                    container.Resolve<GenericRepository<DemoRowPermissions2.DocumentBrowse>>()
                    .Read<Common.RowPermissionsReadItems>();
                Assert.AreEqual("doc1, doc3", TestUtility.DumpSorted(allowedReadBrowse, browse => browse.Title));

                var allowedReadComment =
                    container.Resolve<GenericRepository<DemoRowPermissions2.DocumentComment>>()
                    .Read<Common.RowPermissionsReadItems>();
                Assert.AreEqual("com1", TestUtility.DumpSorted(allowedReadComment, comment => comment.Comment));

                var allowedReadApproval =
                    container.Resolve<GenericRepository<DemoRowPermissions2.DocumentApproval>>()
                    .Read<Common.RowPermissionsReadItems>();
                Assert.AreEqual("app1, app3", TestUtility.DumpSorted(allowedReadApproval, approval => approval.Note));

                var allowedWriteApproval =
                    container.Resolve<GenericRepository<DemoRowPermissions2.DocumentApproval>>()
                    .Read<Common.RowPermissionsWriteItems>();
                Assert.AreEqual("app1", TestUtility.DumpSorted(allowedWriteApproval, approval => approval.Note));
            }
        }

        private void SetBasicAccessPermissions<TEntity>()
        {
            // Set the current user's basic permissions for client requests (not related to row permissions).

            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var genericRepositories = container.Resolve<GenericRepositories>();

                // Loading or creating the Principal record for the current user:
                var userAccount = context.UserInfo.UserName.Split('\\').Last();
                var principal = new Common.Principal { Name = userAccount };
                genericRepositories.InsertOrReadId(principal, p => p.Name);

                // Loading all available claims for the given entity:
                var claims = genericRepositories.Read<Common.Claim>(c => c.ClaimResource == typeof(TEntity).FullName && c.Active.Value);

                // Giving the current user permissions for the claims:
                var permissions = claims.Select(claim => new Common.Permission { Claim = claim, Principal = principal, IsAuthorized = true });
                genericRepositories.InsertOrUpdateReadId(permissions,
                    p => new { cid = p.Claim.ID, pid = p.Principal.ID },
                    p => new { p.IsAuthorized });
            }
        }

        /// <summary>
        /// Parse the server response and generate a simplified report.
        /// </summary>
        private static string GenerateReport(ProcessingResult processingResult)
        {
            if (!processingResult.Success)
                return "ERROR: " + (processingResult.UserMessage ?? processingResult.SystemMessage);

            var commandResult = processingResult.CommandResults.Single();
            if (commandResult.Data == null || commandResult.Data.Value == null)
                return commandResult.Message;

            if (commandResult.Data.Value is ReadCommandResult)
            {
                var records = (IEnumerable<object>)((ReadCommandResult)commandResult.Data.Value).Records;

                if (records is IEnumerable<DemoRowPermissions1.Document>)
                {
                    var documents = (IEnumerable<DemoRowPermissions1.Document>)records;
                    return string.Join(", ", documents.Select(document => document.Title));
                }
                else if (records is IEnumerable<DemoRowPermissions2.Document>)
                {
                    var documents = (IEnumerable<DemoRowPermissions2.Document>)records;
                    return string.Join(", ", documents.Select(document => document.Title));
                }
                else
                    return records.Count() + " records.";
            }
            else
                return "Command result data type " + commandResult.GetType().Name + ".";
        }
    }
}
