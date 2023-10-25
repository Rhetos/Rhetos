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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class SqlProcedureSingleSqlTest
    {
        [TestMethod]
        public void SqlParser()
        {
            var tests = new List<(string module, string fullProcedureSource, string expectedModule, string expectedName, string expectedParams, string expectedBody)>
            {
                ("m", "CREATE PROCEDURE m.p\r\nAS\r\nSELECT 1", "m", "p", "", "SELECT 1"),
                ("m", "create procedure m.p\r\nas\r\nselect 1", "m", "p", "", "select 1"),
                ("m", "CREATE PROCEDURE m.p\nAS\nSELECT 1", "m", "p", "", "SELECT 1"),
                ("m", "CREATE PROCEDURE [m].[p]\r\nAS\r\nSELECT 1", "m", "p", "", "SELECT 1"),
                ("m", "\r\nCREATE PROCEDURE m.p\r\nAS\r\nSELECT 1\r\n", "m", "p", "", "SELECT 1"),
                ("mmm", "CREATE PROCEDURE mmm.ppp\r\nAS\r\nSELECT 1", "mmm", "ppp", "", "SELECT 1"),
                ("mmm", "CREATE OR ALTER PROCEDURE mmm.ppp\r\nAS\r\nSELECT 1", "mmm", "ppp", "", "SELECT 1"),
                ("m", "   CREATE\t\r\n\tPROCEDURE\t   m . p \r\n\r\n  \tAS  \r\n\r\nSELECT 1\r\n\t  \r\n", "m", "p", "", "SELECT 1"),
                ("m", "CREATE PROCEDURE m.p\r\n@OwnerID uniqueidentifier\r\n\r\nAS\r\nSELECT 1", "m", "p", "@OwnerID uniqueidentifier", "SELECT 1"),
                ("m", "CREATE PROCEDURE m.p\r\n(@OwnerID\r\nuniqueidentifier, @ItemID uniqueidentifier)\r\n\r\nAS\r\nSELECT 1", "m", "p", "(@OwnerID\r\nuniqueidentifier, @ItemID uniqueidentifier)", "SELECT 1"),
            };

            foreach ((string module, string fullProcedureSource, string expectedModule, string expectedName, string expectedParams, string expectedBody) in tests)
            {
                var procedure = new SqlProcedureSingleSqlInfo
                {
                    Module = new ModuleInfo { Name = module },
                    FullProcedureSource = fullProcedureSource
                };
                procedure.InitializeNonparsableProperties(out _);
                Assert.AreEqual(
                    $"{expectedModule}, {expectedName}, {expectedParams}, {expectedBody}",
                    $"{procedure.Module}, {procedure.Name}, {procedure.ProcedureArguments}, {procedure.ProcedureSource}");
            }
        }

        [TestMethod]
        public void SqlParserInvalid()
        {
            var tests = new List<(string module, string fullProcedureSource, string expectedException)>
            {
                ("m", "CREATE VIEW", "The SqlProcedure script must start with \"CREATE PROCEDURE\" or \"CREATE OR ALTER PROCEDURE\". Module 'm', SqlProcedure: CREATE VIEW."),
                ("m", "CREATE PROCEDURE --m.p\r\nAS\r\nSELECT 1", "Cannot detect procedure name in the SQL script. Make sure its syntax is correct. Do not use comments before the procedure name."),
                ("m", "CREATE PROCEDURE p\r\nAS\r\nSELECT 1", "SqlProcedure m.p: Procedure 'p' should be named with schema 'm.p', to match the DSL module where the SqlProcedure is placed."),
                ("m", "CREATE PROCEDURE [p]\r\nAS\r\nSELECT 1", "SqlProcedure m.p: Procedure 'p' should be named with schema 'm.p', to match the DSL module where the SqlProcedure is placed."),
                ("m", "CREATE PROCEDURE mmm.p\r\nAS\r\nSELECT 1", "Procedure 'mmm.p' should have schema 'm' instead of 'mmm', to match the DSL module where the SqlProcedure is placed."),
                ("m", "CREATE PROCEDURE m.p\r\nSELECT 1", "Cannot detect beginning of the code block in procedure 'm.p'. Make sure the script contains \"AS\" in its own line."),
                ("m", "CREATE PROCEDURE m.p\r\nAS\r\nSELECT 1\r\nGO\r\nEXEC m.p", "Please remove \"GO\" statement from the SQL script, or use SqlObject instead of SqlProcedure."),
            };

            foreach ((string module, string fullProcedureSource, string expectedException) in tests)
            {
                var procedure = new SqlProcedureSingleSqlInfo
                {
                    Module = new ModuleInfo { Name = module },
                    FullProcedureSource = fullProcedureSource
                };

                var exception = TestUtility.ShouldFail<DslSyntaxException>(() => procedure.InitializeNonparsableProperties(out _));
                TestUtility.AssertContains(exception.Message, expectedException);
            }
        }
    }
}
