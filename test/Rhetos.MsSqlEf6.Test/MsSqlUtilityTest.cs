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
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Rhetos.Utilities;

namespace Rhetos.MsSqlEf6.Test
{
    [TestClass]
    public class MsSqlUtilityTest
    {
        internal static MsSqlUtility NewSqlUtility() => new MsSqlUtility(new NoLocalizer(), new DatabaseSettings { DatabaseLanguage = "MsSql" });

        [TestMethod]
        public void InterpretSqlExceptionUserMessage()
        {
            var msSqlUtility = NewSqlUtility();
            var interpretedException = msSqlUtility.InterpretSqlException(
                NewSqlException("test message", 50000, 16, 101)); // State 101 is Rhetos convention for user error message.
            Assert.AreEqual("UserException: test message", interpretedException.GetType().Name + ": " + interpretedException.Message);
        }

        [TestMethod]
        public void InterpretSqlExceptionWrapping()
        {
            TestInterpretedException(new ListOfTuples<Exception, string>
            {
                { null, null },
                { new FrameworkException("abc"), null },
                { new InvalidOperationException(), null },
                { NewSqlException("test message", 50000, 16, 101), "UserException: test message" },
                { NewSqlException("test message", 50000, 16, 0), null },
                { new InvalidOperationException("ex1", NewSqlException("test message", 50000, 16, 101)), "UserException: test message" },
                { new InvalidOperationException("ex1", NewSqlException("test message", 50000, 16, 0)), null },
                { new InvalidOperationException("ex1", new InvalidOperationException("ex2", NewSqlException("test message", 50000, 16, 101))), "UserException: test message" },
            });
        }

        [TestMethod]
        public void InterpretUniqueConstraint()
        {
            TestInterpretedException(new ListOfTuples<Exception, string>
            {
                // SQL2014:
                {
                    NewSqlException(
                        "Cannot insert duplicate key row in object 'Common.Principal' with unique index 'IX_Principal_Name'. The duplicate key value is (test1).",
                        2601, 14, 1),
                    "UserException: It is not allowed to enter a duplicate record., Constraint=Unique, ConstraintName=IX_Principal_Name, DuplicateValue=test1, Table=Common.Principal"
                },
                // SQL2008, SQL2000:
                {
                    NewSqlException(
                        "Cannot insert duplicate key row in object 'Common.Principal' with unique index 'IX_Principal_Name'.",
                        2601, 14, 1),
                    "UserException: It is not allowed to enter a duplicate record., Constraint=Unique, ConstraintName=IX_Principal_Name, Table=Common.Principal"
                },
            });
        }

        [TestMethod]
        public void InterpretReferenceConstraint()
        {
            TestInterpretedException(new ListOfTuples<Exception, string>
            {
                // SQL2014:
                {
                    NewSqlException(
                        "The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_PrincipalPermission_Principal_PrincipalID\". The conflict occurred in database \"Rhetos\", table \"Common.Principal\", column 'ID'.",
                        547, 16, 0),
                    "UserException: It is not allowed to enter the record. The entered value references nonexistent record."
                        + ", Action=INSERT, Constraint=Reference, ConstraintName=FK_PrincipalPermission_Principal_PrincipalID, ReferencedColumn=ID, ReferencedTable=Common.Principal"
                },
                {
                    NewSqlException(
                        "The UPDATE statement conflicted with the FOREIGN KEY constraint \"FK_PrincipalHasRole_Role_RoleID\". The conflict occurred in database \"Rhetos\", table \"Common.Role\", column 'ID'.",
                        547, 16, 0),
                    "UserException: It is not allowed to edit the record. The entered value references nonexistent record."
                        + ", Action=UPDATE, Constraint=Reference, ConstraintName=FK_PrincipalHasRole_Role_RoleID, ReferencedColumn=ID, ReferencedTable=Common.Role"
                },
                {
                    NewSqlException(
                        "The DELETE statement conflicted with the REFERENCE constraint \"FK_PrincipalHasRole_Role_RoleID\". The conflict occurred in database \"Rhetos\", table \"Common.PrincipalHasRole\", column 'RoleID'.",
                        547, 16, 0),
                    "UserException: It is not allowed to delete a record that is referenced by other records."
                        + ", Action=DELETE, Constraint=Reference, ConstraintName=FK_PrincipalHasRole_Role_RoleID, DependentColumn=RoleID, DependentTable=Common.PrincipalHasRole"
                },
                {
                    NewSqlException(
                        "The DELETE statement conflicted with the SAME TABLE REFERENCE constraint \"FK_PrincipalHasRole_Role_RoleID\". The conflict occurred in database \"Rhetos\", table \"Common.PrincipalHasRole\", column 'RoleID'.",
                        547, 16, 0),
                    "UserException: It is not allowed to delete a record that is referenced by other records."
                        + ", Action=DELETE, Constraint=Reference, ConstraintName=FK_PrincipalHasRole_Role_RoleID, DependentColumn=RoleID, DependentTable=Common.PrincipalHasRole"
                },
                // SQL2000:
                {
                    NewSqlException(
                        "INSERT statement conflicted with COLUMN FOREIGN KEY constraint 'FK1'. The conflict occurred in database 'D1', table 'T1', column 'C1'.",
                        547, 16, 0),
                    "UserException: It is not allowed to enter the record. The entered value references nonexistent record."
                        + ", Action=INSERT, Constraint=Reference, ConstraintName=FK1, ReferencedColumn=C1, ReferencedTable=T1"
                },
            });
        }

        [TestMethod]
        public void InterpretPrimaryKeyConstraint()
        {
            TestInterpretedException(new ListOfTuples<Exception, string>
            {
                // SQL2016:
                {
                    NewSqlException(
                        "Violation of PRIMARY KEY constraint 'PK_Principal'. Cannot insert duplicate key in object 'Common.Principal'. The duplicate key value is (d28a180d-96da-478f-ad7a-e9a071833bad).\r\nThe statement has been terminated.",
                        2627, 14, 1),
                    "FrameworkException: Inserting a record that already exists in database., Constraint=Primary key, ConstraintName=PK_Principal, DuplicateValue=d28a180d-96da-478f-ad7a-e9a071833bad, Table=Common.Principal"
                },
                // SQL2000:
                {
                    NewSqlException(
                        "Violation of PRIMARY KEY constraint 'PK_Principal'. Cannot insert duplicate key in object 'Common.Principal'.",
                        2627, 14, 1),
                    "FrameworkException: Inserting a record that already exists in database., Constraint=Primary key, ConstraintName=PK_Principal, Table=Common.Principal"
                },
            });
        }

        [TestMethod]
        public void InterpretMoneyConstraint()
        {
            TestInterpretedException(new List<(Exception, string, string)>
            {
                (
                    NewSqlException(
                        "The INSERT statement conflicted with the CHECK constraint \"CK_Book_Price_money\". The conflict occurred in database \"rhetos_webapi\", table \"Bookstore.Book\", column 'Price'.",
                        547, 16, 0),
                    "UserException: It is not allowed to enter a money value with more than 2 decimals., Column=Price, Constraint=Money, ConstraintName=CK_Book_Price_money, Table=Bookstore.Book",
                    "DataStructure:Bookstore.Book,Property:Price"
                ),
            });
        }

        private void TestInterpretedException(ListOfTuples<Exception, string> tests)
            => TestInterpretedException(tests.Select(test => (test.Item1, test.Item2, "")).ToList());

        private void TestInterpretedException(List<(Exception, string, string)> tests)
        {
            var msSqlUtility = NewSqlUtility();
            foreach (var test in tests)
            {
                string reportInput = "Input: " + (test.Item1 != null ? test.Item1.ToString() : "null");
                Console.WriteLine(reportInput);
                var interpretedException = msSqlUtility.InterpretSqlException(test.Item1);
                Console.WriteLine("Output: " + (interpretedException != null ? interpretedException.ToString() : "null"));
                Assert.AreEqual(test.Item2, Report(interpretedException), reportInput);
                if (!string.IsNullOrEmpty(test.Item3))
                    Assert.AreEqual(test.Item3, ((UserException)interpretedException).SystemMessage);
            }
        }

        string Report(RhetosException ex)
        {
            if (ex == null)
                return null;

            return ex.GetType().Name + ": " + ex.Message
                + string.Concat(ex.Info.OrderBy(info => info.Key).Select(info => ", " + info.Key + "=" + info.Value.ToString()));
        }

        #region SqlExceptionCreator, thanks to Sam Saffron at http://stackoverflow.com/a/1387030

        internal static SqlException NewSqlException(string message, int number, int level, int state)
        {
            SqlErrorCollection collection = Construct<SqlErrorCollection>();
            int lineNumber = 3;
            Exception exception = null;
            // SqlError constructor parameters from https://github.com/dotnet/SqlClient/blob/9e236792c3b54b51f1dde864f9b6444b56fa1233/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/SqlClient/SqlError.cs
            SqlError error = Construct<SqlError>(number, (byte)state, (byte)level, "test server", message, "test procedure", lineNumber, exception);

            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(collection, new object[] { error });

            return typeof(SqlException)
                .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    CallingConventions.ExplicitThis,
                    new[] { typeof(SqlErrorCollection), typeof(string) },
                    Array.Empty<ParameterModifier>())
                .Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;
        }

        private static T Construct<T>(params object[] p)
        {
            var ctors = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)ctors.Single(ctor => ctor.GetParameters().Length == p.Length).Invoke(p);
        }

        #endregion

        [TestMethod]
        public void ConnectionStringAddAppName()
        {
            string initialConnectionString = "Data Source=DummyServerName;Initial Catalog=DummyDatabaseName;Integrated Security=true;";

            IConfiguration configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue(ConnectionString.ConnectionStringConfigurationKey, initialConnectionString)
                .Build();
            ISqlUtility sqlUtility = NewSqlUtility();
            string rhetosConnectionString = new ConnectionString(configuration, sqlUtility).ToString();

            Assert.AreEqual(
                "Data Source=DummyServerName;Initial Catalog=DummyDatabaseName;Integrated Security=true;Application Name=testhost",
                rhetosConnectionString,
                ignoreCase: true);

            var s = new SqlConnectionStringBuilder(rhetosConnectionString);
            Assert.AreEqual("testhost", s.ApplicationName);
        }

        [TestMethod]
        public void SingleQuote_SimpleTest()
        {
            Assert.AreEqual("N'abc'", NewSqlUtility().QuoteText("abc"));
        }

        [TestMethod]
        public void SingleQuote_EscapeSequenceTest()
        {
            Assert.AreEqual("N'ab''c'", NewSqlUtility().QuoteText("ab'c"));
        }

        [TestMethod]
        public void GetShortName()
        {
            Assert.AreEqual("someview", NewSqlUtility().GetShortName("someschema.someview"));
            Assert.AreEqual("someview", NewSqlUtility().GetShortName("someview"));

            TestUtility.ShouldFail(() => NewSqlUtility().GetShortName("a.b.c"), "Invalid database object name");
            TestUtility.ShouldFail(() => NewSqlUtility().GetShortName("a."), "Invalid database object name");
        }

        [TestMethod]
        public void MsSqlGetSchemaName()
        {
            Assert.AreEqual("someschema", NewSqlUtility().GetSchemaName("someschema.someview"));
            Assert.AreEqual("dbo", NewSqlUtility().GetSchemaName("someview"));
        }

        [TestMethod]
        public void MaskPasswordTest()
        {
            var tests = new ListOfTuples<string, string[]>
            {
                // Format: connection string, expected content.
                { "Server=tcp:name.database.windows.net,1433;Initial Catalog=RhetosAzureDB;Persist Security Info=False;User ID=jjj;Password=jjj;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
                    new[] { "tcp:name.database.windows.net,1433", "RhetosAzureDB" } },
                { "Data Source=localhost;Initial Catalog=Rhetos;Integrated Security=SSPI;",
                    new[] { "localhost", "Rhetos" } },
                { "User Id=jjj;Password=jjj;Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password='jjj;jjj=jjj';Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password=\"jjj;jjj=jjj\";Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "';[]=-jjj",
                    Array.Empty<string>() },
            };

            foreach (var test in tests)
            {
                Console.WriteLine(test.Item1);
                string report;
                try
                {
                    report = NewSqlUtility().SqlConnectionInfo(test.Item1);
                }
                catch (Exception ex)
                {
                    report = $"{ex.GetType()} {ex.Message}";
                }
                Console.WriteLine("=> " + report);

                TestUtility.AssertNotContains(report, "j", "Username or password leaked.");
                if (test.Item2.Any())
                    TestUtility.AssertContains(report, test.Item2);
            }
        }

        [TestMethod]
        public void QuoteIdentifier()
        {
            var tests = new Dictionary<string, string>
            {
                { "abc", "[abc]" },
                { "abc[", "[abc[]" },
                { "abc]", "[abc]]]" },
                { "[][][]", "[[]][]][]]]" },
                { " '\" ", "[ '\" ]" }
            };

            foreach (var test in tests)
                Assert.AreEqual(test.Value, NewSqlUtility().QuoteIdentifier(test.Key));
        }

        [TestMethod]
        public void InvalidConnectionStringFormat()
        {
            string invalidConnectionString = "<ENTER_CONNECTION_STRING_HERE>";

            var ex = TestUtility.ShouldFail<ArgumentException>(
                () => NewSqlUtility().ValidateDbConnection(invalidConnectionString),
                "Database connection string has invalid format",
                "ConnectionStrings:RhetosConnectionString");

            TestUtility.AssertNotContains(
                ex.ToString(),
                new[] { invalidConnectionString },
                "The connection string should not be reported in the error message or error log, because it could contain a password.");
        }
    }
}
