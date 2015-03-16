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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryWithParameterInfo))]
    public class QueryWithParameterCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<QueryWithParameterInfo> BeforeQueryTag = "BeforeQuery";
        public static readonly CsTag<QueryWithParameterInfo> AfterQueryTag = "AfterQuery";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryWithParameterInfo)conceptInfo;

            codeBuilder.InsertCode(InterfaceName(info), RepositoryHelper.RepositoryInterfaces, info.DataStructure);
            codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info.DataStructure);
            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }

        private static string MemberFunctionsSnippet(QueryWithParameterInfo info)
        {
            return string.Format(
@"        public IQueryable<global::{0}.{1}> Query({2} queryParameter)
        {{
            {4}
            Func<{2}, IQueryable<global::{0}.{1}>> queryFunction = {3};
            var queryResult = queryFunction(queryParameter);
            {5}
            return queryResult;
        }}

",
                info.DataStructure.Module.Name,
                info.DataStructure.Name,
                info.ParameterType,
                info.QueryImplementation.Trim(),
                BeforeQueryTag.Evaluate(info),
                AfterQueryTag.Evaluate(info));
        }

        private static string RegisterRepository(QueryWithParameterInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().As<{2}>().InstancePerLifetimeScope();
            ", info.DataStructure.Module.Name, info.DataStructure.Name, InterfaceName(info));
        }

        private static string InterfaceName(QueryWithParameterInfo info)
        {
            return string.Format("IQueryableRepository<global::{0}.{1}, {2}>", info.DataStructure.Module.Name, info.DataStructure.Name, info.ParameterType);
        }
    }
}
