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
using Rhetos.Dom.DefaultConcepts.Persistence;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class FullTextSearchWithRankInterceptorTest
    {
        [TestMethod]
        public void Simple()
        {
            string generatedQuery = @"
SELECT 
    CASE WHEN ([Extent1].[Code] IS NULL) THEN N'' ELSE  CAST( [Extent1].[Code] AS nvarchar(max)) END + N'-' + CASE WHEN ([Extent1].[Name] IS NULL) THEN N'' ELSE [Extent1].[Name] END AS [C1]
    FROM  [TestFullTextSearch].[Simple] AS [Extent1]
    LEFT OUTER JOIN [TestFullTextSearch].[Simple_Search] AS [Extent2] ON [Extent1].[ID] = [Extent2].[ID]
    WHERE ([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID], @p__linq__0, @p__linq__1, N'TestFullTextSearch.Simple_Search', N'*')) = 1 AND ( NOT (([Extent1].[ID] = [Extent2].[ID]) AND (0 = (CASE WHEN ([Extent2].[ID] IS NULL) THEN cast(1 as bit) ELSE cast(0 as bit) END))))
	
AND ([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1";

            var command = new SqlCommand(generatedQuery);
            new FullTextSearchWithRankInterceptor().ReaderExecuting(command, null);

            string expected = @"
SELECT 
    CASE WHEN ([Extent1].[Code] IS NULL) THEN N'' ELSE  CAST( [Extent1].[Code] AS nvarchar(max)) END + N'-' + CASE WHEN ([Extent1].[Name] IS NULL) THEN N'' ELSE [Extent1].[Name] END AS [C1]
    FROM  [TestFullTextSearch].[Simple] AS [Extent1]
    LEFT OUTER JOIN [TestFullTextSearch].[Simple_Search] AS [Extent2] ON [Extent1].[ID] = [Extent2].[ID]
    WHERE [Extent1].[ID] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search, *, @p__linq__0, @p__linq__1)) AND ( NOT (([Extent1].[ID] = [Extent2].[ID]) AND (0 = (CASE WHEN ([Extent2].[ID] IS NULL) THEN cast(1 as bit) ELSE cast(0 as bit) END))))
	
AND [Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, (a, b, c), N'a2', 33))";


            Assert.AreEqual(expected, command.CommandText);
        }

        [TestMethod]
        public void ValidQueries()
        {
            var tests = new Dictionary<string, string>
            {
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, (a, b, c), N'a2', 33))"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'abc')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, abc, N'a2', 33))"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'*')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, *, N'a2', 33))"
                },
            };

            foreach (var test in tests)
            {
                var command = new SqlCommand(test.Key);
                new FullTextSearchWithRankInterceptor().ReaderExecuting(command, null);
                Assert.AreEqual(test.Value, command.CommandText);
            }
        }
        
        [TestMethod]
        public void InvalidQueries()
        {
            var tests = new Dictionary<string, string>
            {
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank](@id, N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'itemId' parameter format"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a'a2', 33, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'pattern' parameter format"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], Na2, 33, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'pattern' parameter format"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch'Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'tableName' parameter format"
                },
                {
                    @"([Rhetos].[InterceptFullTextSearchWithRank]([Extent1].[ID2], N'a2', 33, N'TestFullTextSearch.Simple_Search2', N'a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'searchColumns' parameter format"
                },
                {
                    @"InterceptFullTextSearchWithRank",
                    @"Not all search conditions were handled"
                },

            };

            foreach (var test in tests)
            {
                var command = new SqlCommand(test.Key);
                TestUtility.ShouldFail<FrameworkException>(
                    () => new FullTextSearchWithRankInterceptor().ReaderExecuting(command, null),
                    test.Value);
            }
        }
    }
}
