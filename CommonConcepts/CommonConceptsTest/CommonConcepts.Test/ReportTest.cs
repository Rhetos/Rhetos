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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using TestReport;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ReportTest
    {
        [TestMethod]
        public void MultipleSources()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestReport.Document" });
                var repository = container.Resolve<Common.DomRepository>();

                var d1 = new Document { ID = Guid.NewGuid(), Name = "d1" };
                var d2 = new Document { ID = Guid.NewGuid(), Name = "d2" };
                var d3 = new Document { ID = Guid.NewGuid(), Name = "d3" };
                var d4 = new Document { ID = Guid.NewGuid(), Name = "d4" };
                repository.TestReport.Document.Insert(new[] { d1, d2, d3, d4 });

                var s11 = new Part1 { ParentID = d1.ID, Name = "A s11" };
                var s12 = new Part1 { ParentID = d2.ID, Name = "A s12" };
                var s13 = new Part1 { ParentID = d1.ID, Name = "A s13" };
                var s14 = new Part1 { ParentID = d3.ID, Name = "A s14" };
                var s15 = new Part1 { ParentID = d4.ID, Name = "B s15" };
                repository.TestReport.Part1.Insert(new[] { s11, s12, s13, s14, s15 });

                var s21 = new Part2 { ParentID = d1.ID, Name = "s21" };
                var s22 = new Part2 { ParentID = d3.ID, Name = "s22 xx" };
                var s23 = new Part2 { ParentID = d3.ID, Name = "s23 xxx" };
                var s24 = new Part2 { ParentID = d3.ID, Name = "s24 x" };
                var s25 = new Part2 { ParentID = d4.ID, Name = "s25" };
                repository.TestReport.Part2.Insert(new[] { s21, s22, s23, s24, s25 });

                var reportData = repository.TestReport.MultipleSourcesReport.GetReportData(
                    new MultipleSourcesReport { Part1Prefix = "A" });

                var reportDump = string.Join("|", reportData.Select(group =>
                    string.Join(", ", group.Select(item => ((dynamic)item).Name))));

                const string expectedReport1 = "d3, d2, d1|s24 x, s22 xx, s23 xxx, s21|A s14, A s13, A s12, A s11";
                const string expectedReport2 = "d3, d1, d2|s24 x, s22 xx, s23 xxx, s21|A s14, A s13, A s12, A s11"; // Order of d1 and d2 is not defined.

                Console.WriteLine("Report result: " + reportDump);
                Console.WriteLine(expectedReport1);
                Console.WriteLine(expectedReport2);

                Assert.IsTrue(
                    new[] { expectedReport1, expectedReport2 }.Contains(reportDump),
                    "Report result should be one of the expected results.");
            }
        }

        [TestMethod]
        public void CustomReportFile()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestReport.Document" });
                var repository = container.Resolve<Common.DomRepository>();

                var d1 = new Document { ID = Guid.NewGuid(), Name = "d1" };
                var d2 = new Document { ID = Guid.NewGuid(), Name = "d2" };
                repository.TestReport.Document.Insert(new[] { d1, d2 });

                var s11 = new Part1 { ParentID = d1.ID, Name = "A s11" };
                var s12 = new Part1 { ParentID = d1.ID, Name = "B s12" };
                var s13 = new Part1 { ParentID = d2.ID, Name = "B s13" };
                repository.TestReport.Part1.Insert(new[] { s11, s12, s13 });

                var report = repository.TestReport.CustomFileReport.GenerateReport(new CustomFileReport { Prefix = "B" });

                Assert.AreEqual("CustomFileReport.txt", report.Name);
                Assert.AreEqual("d1, d2|B s12, B s13", new UTF8Encoding().GetString(report.Content));
            }
        }
    }
}
