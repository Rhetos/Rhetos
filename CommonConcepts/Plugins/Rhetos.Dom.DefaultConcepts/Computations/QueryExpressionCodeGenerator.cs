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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryExpressionInfo))]
    public class QueryExpressionCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<QueryExpressionInfo> BeforeQueryTag = "BeforeQuery";
        public static readonly CsTag<QueryExpressionInfo> AfterQueryTag = "AfterQuery";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryExpressionInfo)conceptInfo;

            codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info.DataStructure);
        }

        private static string MemberFunctionsSnippet(QueryExpressionInfo info)
        {
            return string.Format(
        @"public IQueryable<Common.Queryable.{0}_{1}> Query({2} queryParameter)
        {{
            {4}
            Func<{2}, IQueryable<Common.Queryable.{0}_{1}>> queryFunction = {3};
            var queryResult = queryFunction(queryParameter);
            {5}
            return queryResult;
        }}

        ",
                info.DataStructure.Module.Name,
                info.DataStructure.Name,
                info.Parameter,
                info.QueryImplementation.Trim(),
                BeforeQueryTag.Evaluate(info),
                AfterQueryTag.Evaluate(info));
        }
    }
}
