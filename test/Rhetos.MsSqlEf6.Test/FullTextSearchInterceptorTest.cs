﻿/*
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

using Rhetos.Dom.DefaultConcepts.Persistence;
using Rhetos.Persistence;
using System.Data.SqlClient;

namespace Rhetos.MsSqlEf6.Test
{
    public class FullTextSearchInterceptorTest
    {
        [Fact]
        public void Simple()
        {
            string generatedQuery = $@"
SELECT 
    CASE WHEN ([Extent1].[Code] IS NULL) THEN N'' ELSE  CAST( [Extent1].[Code] AS nvarchar(max)) END + N'-' + CASE WHEN ([Extent1].[Name] IS NULL) THEN N'' ELSE [Extent1].[Name] END AS [C1]
    FROM  [TestFullTextSearch].[Simple] AS [Extent1]
    LEFT OUTER JOIN [TestFullTextSearch].[Simple_Search] AS [Extent2] ON [Extent1].[ID] = [Extent2].[ID]
    WHERE ([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID], @p__linq__0, N'TestFullTextSearch.Simple_Search', N'*')) = 1 AND ( NOT (([Extent1].[ID] = [Extent2].[ID]) AND (0 = (CASE WHEN ([Extent2].[ID] IS NULL) THEN cast(1 as bit) ELSE cast(0 as bit) END))))
	
AND ([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1";

            using (var command = new SqlCommand(generatedQuery))
            {
                new FullTextSearchInterceptor().ReaderExecuting(command, null);

                string expected = @"
SELECT 
    CASE WHEN ([Extent1].[Code] IS NULL) THEN N'' ELSE  CAST( [Extent1].[Code] AS nvarchar(max)) END + N'-' + CASE WHEN ([Extent1].[Name] IS NULL) THEN N'' ELSE [Extent1].[Name] END AS [C1]
    FROM  [TestFullTextSearch].[Simple] AS [Extent1]
    LEFT OUTER JOIN [TestFullTextSearch].[Simple_Search] AS [Extent2] ON [Extent1].[ID] = [Extent2].[ID]
    WHERE [Extent1].[ID] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search, *, @p__linq__0)) AND ( NOT (([Extent1].[ID] = [Extent2].[ID]) AND (0 = (CASE WHEN ([Extent2].[ID] IS NULL) THEN cast(1 as bit) ELSE cast(0 as bit) END))))
	
AND [Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, (a, b, c), N'a2'))";


                Assert.Equal(expected, command.CommandText);
            }
        }

        [Fact]
        public void ValidQueries()
        {
            var tests = new Dictionary<string, string>
            {
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, (a, b, c), N'a2'))"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch.Simple_Search2', N'abc')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, abc, N'a2'))"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch.Simple_Search2', N'*')) = 1",
                    @"[Extent1].[ID2] IN (SELECT [KEY] FROM CONTAINSTABLE(TestFullTextSearch.Simple_Search2, *, N'a2'))"
                },
            };

            foreach (var test in tests)
            {
                using (var command = new SqlCommand(test.Key))
                {
                    new FullTextSearchInterceptor().ReaderExecuting(command, null);
                    Assert.Equal(test.Value, command.CommandText);
                }
            }
        }

        [Fact]
        public void InvalidQueries()
        {
            var tests = new Dictionary<string, string>
            {
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch](@id, N'a2', N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'itemId' parameter format"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a'a2', N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'pattern' parameter format"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], Na2, N'TestFullTextSearch.Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'pattern' parameter format"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch'Simple_Search2', N'(a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'tableName' parameter format"
                },
                {
                    $@"([{EntityFrameworkMapping.StorageModelNamespace}].[InterceptFullTextSearch]([Extent1].[ID2], N'a2', N'TestFullTextSearch.Simple_Search2', N'a, b, c)')) = 1",
                    @"Invalid FullTextSearch 'searchColumns' parameter format"
                },
                {
                    @"InterceptFullTextSearch",
                    @"Not all search conditions were handled"
                },

            };

            foreach (var test in tests)
            {
                using (var command = new SqlCommand(test.Key))
                {
                    var ex = Assert.Throws<FrameworkException>(() => new FullTextSearchInterceptor().ReaderExecuting(command, null));
                    Assert.Contains(test.Value, ex.ToString());
                }
            }
        }
    }
}
