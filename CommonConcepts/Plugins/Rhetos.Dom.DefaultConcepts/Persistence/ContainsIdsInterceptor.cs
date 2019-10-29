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

using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rhetos.Dom.DefaultConcepts
{
    public class ContainsIdsInterceptor : IDbCommandInterceptor
    {
        public ContainsIdsInterceptor()
        { }

        public void NonQueryExecuting(
            DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {

        }

        public void NonQueryExecuted(
            DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {

        }

        public void ReaderExecuting(
            DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            RewriteQuerry(command);
        }

        public void ReaderExecuted(
            DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {

        }

        public void ScalarExecuting(
            DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            RewriteQuerry(command);
        }

        public void ScalarExecuted(
            DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {

        }

        private static void RewriteQuerry(DbCommand cmd)
        {
            if (cmd.CommandText.Contains(EFExpression.ContainsIdsFunction))
            {
                var parseContainsIdsQuery = new Regex($@"\(\[{EntityFrameworkMapping.StorageModelNamespace}\]\.\[{EFExpression.ContainsIdsFunction}\]\((?<id>.+?), (?<concatenatedIds>.*?)\)\) = 1", RegexOptions.Singleline);

                var containsIdsQueries = parseContainsIdsQuery.Matches(cmd.CommandText).Cast<Match>();

                foreach (var containsIdsQuery in containsIdsQueries.OrderByDescending(m => m.Index))
                {
                    string id = containsIdsQuery.Groups["id"].Value;
                    string concatenatedIds = containsIdsQuery.Groups["concatenatedIds"].Value;

                    var indexofConcatenatedIdsParameter = cmd.Parameters.IndexOf(concatenatedIds.Replace("@", ""));
                    var concatentaedIdsParameterValue = cmd.Parameters[indexofConcatenatedIdsParameter].Value as string;

                    string containsIdsSql;
                    if (string.IsNullOrEmpty(concatentaedIdsParameterValue))
                        containsIdsSql = "1 = 0";
                    else
                        containsIdsSql = string.Format("{0} IN ({1})", id, string.Join(",", concatentaedIdsParameterValue.Split(',').Select(x => "'" + x + "'")));

                    cmd.CommandText =
                        cmd.CommandText.Substring(0, containsIdsQuery.Index)
                        + containsIdsSql
                        + cmd.CommandText.Substring(containsIdsQuery.Index + containsIdsQuery.Length);

                    cmd.Parameters.RemoveAt(indexofConcatenatedIdsParameter);
                }
            }

            if (cmd.CommandText.Contains(EFExpression.ContainsIdsFunction))
                throw new FrameworkException("Error while parsing ContainsIds query. Not all conditions were handled.");
        }
    }
}
