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
using System.Collections.Generic;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class SqlFunctionSingleSqlTest
    {
        [TestMethod]
        public void SqlParser()
        {
            var tests = new List<(string module, string fullFunctionSource, string expectedModule, string expectedName, string expectedParams, string expectedBody)>
            {
                ("dbo",
@"
CREATE FUNCTION dbo.ISOweek (@DATE DATETIME)
RETURNS INT
WITH EXECUTE AS CALLER
AS
BEGIN
    DECLARE @ISOweek INT;
    RETURN (@ISOweek);
END;
GO


"
                , "dbo", "ISOweek", "@DATE DATETIME", "RETURNS INT\r\nWITH EXECUTE AS CALLER\r\nAS\r\nBEGIN\r\n    DECLARE @ISOweek INT;\r\n    RETURN (@ISOweek);\r\nEND;\r\nGO"),
                ("Sales",
@"
CREATE FUNCTION Sales.ufn_SalesByStore (@storeid INT)
RETURNS TABLE
AS
RETURN (
    SELECT a=1
);

                ", "Sales", "ufn_SalesByStore", "@storeid INT", "RETURNS TABLE\r\nAS\r\nRETURN (\r\n    SELECT a=1\r\n);"),
                ("dbo",
@"
CREATE FUNCTION dbo.ufn_FindReports (
@InEmpID
INT
)
RETURNS @retFindReports TABLE (
    EmployeeID INT PRIMARY KEY NOT NULL, -- Line comment!
    )
    --Returns a result set that lists all the employees who report to the
    --specific employee directly or indirectly.
AS
BEGIN
    invalidbody
END;
", "dbo", "ufn_FindReports", "\r\n@InEmpID\r\nINT\r\n", "RETURNS @retFindReports TABLE (\r\n    EmployeeID INT PRIMARY KEY NOT NULL, -- Line comment!\r\n    )\r\n    --Returns a result set that lists all the employees who report to the\r\n    --specific employee directly or indirectly.\r\nAS\r\nBEGIN\r\n    invalidbody\r\nEND;"),

                ("m", "CREATE FUNCTION m.p\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1", "m", "p", "", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("m", "create function m.p\r\n() returns table as return \r\nselect 1", "m", "p", "", "returns table as return \r\nselect 1"),
                ("m", "CREATE FUNCTION m.p\n() RETURNS TABLE AS RETURN \nSELECT 1", "m", "p", "", "RETURNS TABLE AS RETURN \nSELECT 1"),
                ("m", "CREATE FUNCTION [m].[p]\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1", "m", "p", "", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("m", "\r\nCREATE FUNCTION m.p\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1\r\n", "m", "p", "", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("mmm", "CREATE FUNCTION mmm.ppp\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1", "mmm", "ppp", "", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("mmm", "CREATE OR ALTER FUNCTION mmm.ppp\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1", "mmm", "ppp", "", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("m", "   CREATE\t\r\n\tFUNCTION\t   m . p \r\n\r\n  \t() RETURNS TABLE AS RETURN   \r\n\r\nSELECT 1\r\n\t  \r\n", "m", "p", "", "RETURNS TABLE AS RETURN   \r\n\r\nSELECT 1"),
                ("m", "CREATE FUNCTION m.p\r\n(@OwnerID uniqueidentifier\r\n)\r\n\r\nRETURNS TABLE AS RETURN \r\nSELECT 1", "m", "p", "@OwnerID uniqueidentifier\r\n", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("m", "CREATE FUNCTION m.p\r\n(@OwnerID\r\nuniqueidentifier, @ItemID uniqueidentifier)\r\n\r\n RETURNS TABLE AS RETURN \r\nSELECT 1", "m", "p", "@OwnerID\r\nuniqueidentifier, @ItemID uniqueidentifier", "RETURNS TABLE AS RETURN \r\nSELECT 1"),
                ("m", "CREATE FUNCTION m.p\r\n(@OwnerID uniqueidentifier, @ItemID uniqueidentifier)\r\n\r\n RETURNS TABLE AS RETURN \r\nSELECT 1", "m", "p", "@OwnerID uniqueidentifier, @ItemID uniqueidentifier", "RETURNS TABLE AS RETURN \r\nSELECT 1"),

            };

            foreach ((string module, string fullFunctionSource, string expectedModule, string expectedName, string expectedParams, string expectedBody) in tests)
            {
                var function = new SqlFunctionSingleSqlInfo
                {
                    Module = new ModuleInfo { Name = module },
                    FullFunctionSource = fullFunctionSource
                };
                function.InitializeNonparsableProperties(out _);
                Assert.AreEqual(
                    $"{expectedModule}, {expectedName}, {expectedParams}, {expectedBody}",
                    $"{function.Module}, {function.Name}, {function.Arguments}, {function.Source}");
            }
        }

        [TestMethod]
        public void SqlParserInvalid()
        {
            var tests = new List<(string module, string fullFunctionSource, string expectedException)>
            {
                ("m", "CREATE VIEW", "The SqlFunction script must start with \"CREATE FUNCTION\" or \"CREATE OR ALTER FUNCTION\". Module 'm', SqlFunction: CREATE VIEW."),
                ("m", "CREATE FUNCTION m.p\r\n@OwnerID uniqueidentifier\r\n\r\n() RETURNS TABLE AS RETURN \r\nSELECT 1", "Cannot detect beginning of the parameters and code block in function 'm.p'. Make sure the SQL script has a valid syntax."),
                ("m", "CREATE FUNCTION --m.p\r\nAS\r\nSELECT 1", "Cannot detect function name in the SQL script. Make sure its syntax is correct. Do not use comments before the function name."),
                ("m", "CREATE FUNCTION p\r\nAS\r\nSELECT 1", "SqlFunction m.p: SqlFunction 'p' should be named with schema 'm.p', to match the DSL module where the SqlFunction is placed."),
                ("m", "CREATE FUNCTION [p]\r\nAS\r\nSELECT 1", "SqlFunction m.p: SqlFunction 'p' should be named with schema 'm.p', to match the DSL module where the SqlFunction is placed."),
                ("m", "CREATE FUNCTION mmm.p\r\nAS\r\nSELECT 1", "SqlFunction 'mmm.p' should have schema 'm' instead of 'mmm', to match the DSL module where the SqlFunction is placed."),
                ("m", "CREATE FUNCTION m.p\r\nSELECT 1", "Cannot detect beginning of the parameters and code block in function 'm.p'. Make sure the SQL script has a valid syntax."),
                ("m", "CREATE FUNCTION m.p()\r\nRETURNS TABLE AS RETURN\r\nSELECT 1\r\nGO\r\nEXEC m.p", "Please remove \"GO\" statement from the SQL script, or use SqlObject instead of SqlFunction."),
            };

            foreach ((string module, string fullFunctionSource, string expectedException) in tests)
            {
                var function = new SqlFunctionSingleSqlInfo
                {
                    Module = new ModuleInfo { Name = module },
                    FullFunctionSource = fullFunctionSource
                };

                TestUtility.ShouldFail<DslSyntaxException>(() => function.InitializeNonparsableProperties(out _), expectedException);
            }
        }
    }
}
