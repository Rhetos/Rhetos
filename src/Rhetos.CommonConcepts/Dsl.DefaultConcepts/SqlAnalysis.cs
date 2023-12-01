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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    public static class SqlAnalysis
    {
        private static Dictionary<string, SortedSet<string>> SqlObjectsCache = new Dictionary<string, SortedSet<string>>();

        public static IEnumerable<IConceptInfo> GenerateDependencies(IConceptInfo dependent, IDslModel existingConcepts, string sqlScript)
        {
            SortedSet<string> sqlObjectsInScript;
            if (!SqlObjectsCache.TryGetValue(sqlScript, out sqlObjectsInScript))
            {
                sqlObjectsInScript = new SortedSet<string>(ExtractPossibleObjects(sqlScript), StringComparer.InvariantCultureIgnoreCase);
                SqlObjectsCache.Add(sqlScript, sqlObjectsInScript);
            }

            var newConcepts = new List<IConceptInfo>();

            var conceptsBySqlName = existingConcepts.GetIndex<SqlObjectsIndex>().ConceptsBySqlName;

            foreach (var sqlObjectInScript in sqlObjectsInScript)
                foreach (var conceptInfo in conceptsBySqlName.Get(sqlObjectInScript))
                    if (conceptInfo != dependent)
                    {
                        if (conceptInfo is DataStructureInfo dataStructure)
                            newConcepts.Add(new SqlDependsOnDataStructureInfo { Dependent = dependent, DependsOn = dataStructure });
                        else if (conceptInfo is SqlViewInfo sqlView)
                            newConcepts.Add(new SqlDependsOnSqlViewInfo { Dependent = dependent, DependsOn = sqlView });
                        else if (conceptInfo is SqlFunctionInfo sqlFunction)
                            newConcepts.Add(new SqlDependsOnSqlFunctionInfo { Dependent = dependent, DependsOn = sqlFunction });
                        else if (conceptInfo is SqlObjectInfo sqlObject)
                            newConcepts.Add(new SqlDependsOnSqlObjectInfo { Dependent = dependent, DependsOn = sqlObject });
                        else
                            throw new DslConceptSyntaxException(dependent, "Internal error: Unexpected SQL concept type: " + conceptInfo.GetUserDescription() + ".");
                    }

            return newConcepts;
        }

        public static IEnumerable<IConceptInfo> GenerateDependenciesToObject(IConceptInfo dependent, IDslModel existingConcepts, string sqlObjectName)
        {
            var newConcepts = new List<IConceptInfo>();

            sqlObjectName = sqlObjectName.Trim();
            bool function = sqlObjectName.Contains("(");
            if (function)
                sqlObjectName = sqlObjectName.Substring(0, sqlObjectName.IndexOf('('));

            var nameParts = sqlObjectName.Split('.');
            if (nameParts.Length != 2)
                return newConcepts;

            if (function)
            {
                newConcepts.AddRange(existingConcepts.FindByType<SqlFunctionInfo>()
                    .Where(ci => ci.Module.Name.Equals(nameParts[0], StringComparison.OrdinalIgnoreCase)
                        && ci.Name.Equals(nameParts[1], StringComparison.OrdinalIgnoreCase)
                        && ci != dependent)
                    .Select(ci => new SqlDependsOnSqlFunctionInfo { Dependent = dependent, DependsOn = ci }));
            }
            else
            {
                newConcepts.AddRange(existingConcepts.FindByType<DataStructureInfo>()
                    .Where(ci => ci.Module.Name.Equals(nameParts[0], StringComparison.OrdinalIgnoreCase)
                        && ci.Name.Equals(nameParts[1], StringComparison.OrdinalIgnoreCase)
                        && ci != dependent)
                    .Select(ci => new SqlDependsOnDataStructureInfo { Dependent = dependent, DependsOn = ci }));

                newConcepts.AddRange(existingConcepts.FindByType<SqlViewInfo>()
                    .Where(ci => ci.Module.Name.Equals(nameParts[0], StringComparison.OrdinalIgnoreCase)
                        && ci.Name.Equals(nameParts[1], StringComparison.OrdinalIgnoreCase)
                        && ci != dependent)
                    .Select(ci => new SqlDependsOnSqlViewInfo { Dependent = dependent, DependsOn = ci }));
            }

            return newConcepts;
        }

        static readonly char[] SectionStartChars = new[] { '/', '-', '"', '[', '\'' };
        static readonly char[] EolChars = new[] { '\r', '\n' };
        static readonly char[] MultilineCommentChars = new[] { '/', '*' };

        public static string RemoveCommentsAndText(string sql)
        {
            var result = new StringBuilder(sql.Length);
            int begin = 0;
            int end = 0;

            while (true)
            {
                int sectionStart = end = sql.IndexOfAny(SectionStartChars, end);
                if (end == -1)
                    break;
                char c = sql[end++];
                switch (c)
                {
                    case '"':
                        end = sql.IndexOf('"', end);
                        if (end != -1) end++;
                        break;
                    case '[':
                        while (true)
                        {
                            end = sql.IndexOf(']', end);
                            if (end != -1) end++;
                            if (TryGet(sql, end) == ']') { end++; continue; }
                            break;
                        }
                        break;
                    case '\'':
                        result.Append(sql, begin, sectionStart - begin);
                        end = sql.IndexOf('\'', end);
                        if (end != -1) end++;
                        begin = end;
                        break;
                    case '-':
                        if (TryGet(sql, end) == '-') end++; else break;
                        result.Append(sql, begin, sectionStart - begin);
                        end = sql.IndexOfAny(EolChars, end);
                        begin = end;
                        break;
                    case '/':
                        if (TryGet(sql, end) == '*') end++; else break;
                        result.Append(sql, begin, sectionStart - begin);
                        int depth = 1;
                        while (depth > 0)
                        {
                            end = sql.IndexOfAny(MultilineCommentChars, end);
                            if (end == -1) break;
                            char first = sql[end++];
                            if (first == '/')
                            {
                                if (TryGet(sql, end) == '*') end++; else continue;
                                depth++;
                            }
                            else
                            {
                                if (TryGet(sql, end) == '/') end++; else continue;
                                depth--;
                            }
                        }
                        begin = end;
                        break;
                    default: throw new FrameworkException("Unexpected match pattern '" + c + "' at position " + sectionStart + " on SQL dependency analysis:\r\n" + sql);
                }
                if (end == -1)
                    break;
            }
            if (begin != -1)
                result.Append(sql, begin, sql.Length - begin);
            return result.ToString();
        }

        private static char TryGet(string sql, int index)
        {
            if (index >= 0 && index < sql.Length)
                return sql[index];
            return '\0';
        }

        private const string sqlIdentifier = @"\b\w+\b|\[\w+\]|\""\w+\"""; // Use of special characters inside the bracket is not supported. DependsOn can be manually created for such objects.
        private static readonly string sqlName = $@"(?<schema>{sqlIdentifier})\s*\.\s*(?<name>{sqlIdentifier})";

        private static readonly Regex simpleUsageRegex = new Regex(@"\b(FROM|JOIN|INTO|MERGE|USING)\s+" + sqlName, RegexOptions.IgnoreCase);
        private static readonly Regex scalarFunctionRegex = new Regex(sqlName + @"\s*\(", RegexOptions.RightToLeft);
        private static readonly Regex crossJoinFromRegex = new Regex(@"\bFROM\b", RegexOptions.IgnoreCase);
        private static readonly Regex crossJoinRegex = new Regex(@",\s*" + sqlName);
        private static readonly Regex identifierRegex = new Regex(sqlIdentifier);

        private static List<string> ExtractPossibleObjects(string sql)
        {
            sql = RemoveCommentsAndText(sql);

            var sqlObjects = new List<string>();

            Extract(sql, sqlObjects, simpleUsageRegex);
            Extract(sql, sqlObjects, scalarFunctionRegex, startPosition: sql.Length); // Reverse start position, regarding RegexOptions.RightToLeft option.

            var firstFrom = crossJoinFromRegex.Match(sql);
            if (firstFrom.Success)
                Extract(sql, sqlObjects, crossJoinRegex, firstFrom.Index + firstFrom.Length);

            return sqlObjects.Distinct().OrderBy(x => x).ToList();
        }

        private static void Extract(string sql, List<string> sqlObjects, Regex regex, int startPosition = 0)
        {
            var matches = regex.Matches(sql, startPosition);
            for (int m = 0; m < matches.Count; m++)
            {
                var groups = matches[m].Groups;
                sqlObjects.Add(RemoveQuotes(groups["schema"].Value) + "." + RemoveQuotes(groups["name"].Value));
            }
        }

        private static string RemoveQuotes(string name)
        {
            if (name.StartsWith("[") || name.StartsWith("\""))
                return name.Substring(1, name.Length - 2);
            return name;
        }

        public static List<string> ExtractPossibleColumnNames(string sql)
        {
            sql = RemoveCommentsAndText(sql);
            return identifierRegex.Matches(sql).Cast<Match>()
                .Select(match => RemoveQuotes(match.Value))
                .Distinct().ToList();
        }
    }
}
