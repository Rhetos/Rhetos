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
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RepositoryHelper
    {
        public static readonly CsTag<DataStructureInfo> RepositoryAttributes = "RepositoryAttributes";
        public static readonly CsTag<DataStructureInfo> RepositoryInterfaces = new CsTag<DataStructureInfo>("RepositoryInterface", TagType.Appendable, ",\r\n        {0}");
        public static readonly CsTag<DataStructureInfo> RepositoryMembers = "RepositoryMembers";
        public static readonly CsTag<DataStructureInfo> QueryLoadedAssignPropertyTag = "QueryLoadedAssignProperty";
        public static readonly CsTag<DataStructureInfo> QueryLoadedConstructorTag = "QueryLoadedConstructor";

        private static string RepositorySnippet(DataStructureInfo info)
        {
            return string.Format(
@"{1}
    public class {0}_Repository : IRepository{2}
    {{
        private readonly Common.DomRepository _domRepository;
        private readonly Common.ExecutionContext _executionContext;

        public {0}_Repository(Common.DomRepository domRepository, Common.ExecutionContext executionContext)
        {{
            _domRepository = domRepository;
            _executionContext = executionContext;
        }}

{3}
    }}

", info.Name, RepositoryAttributes.Evaluate(info), RepositoryInterfaces.Evaluate(info), RepositoryMembers.Evaluate(info));
        }

        private static string CallFromModuleRepostiorySnippet(DataStructureInfo info)
        {
            return string.Format(
@"        private {0}_Repository _{0}_Repository;
        public {0}_Repository {0} {{ get {{ return _{0}_Repository ?? (_{0}_Repository = new {0}_Repository(_domRepository, _executionContext)); }} }}

", info.Name);
        }

        private static string RegisterRepository(DataStructureInfo info)
        {
            return string.Format(
            @"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IRepository>(""{0}.{1}"").InstancePerLifetimeScope();
            ",
                info.Module.Name,
                info.Name);
        }

        public static void GenerateRepository(DataStructureInfo info, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(RepositorySnippet(info), ModuleCodeGenerator.HelperNamespaceMembersTag, info.Module);
            codeBuilder.InsertCode(CallFromModuleRepostiorySnippet(info), ModuleCodeGenerator.RepositoryMembersTag, info.Module);
            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
        
        //==============================================================

        public static readonly CsTag<DataStructureInfo> BeforeQueryTag = "RepositoryBeforeQuery";

        private static string RepositoryReadFunctionsSnippet(DataStructureInfo info, string readFunctionBody)
        {
            return string.Format(
@"        public IEnumerable<{0}> Load(object parameter, Type parameterType)
        {{
            throw new NotImplementedException(""Use any of Load or Filter functions, or use GenericRepository class instead."");
        }}

        public IEnumerable<{0}> Read(object parameter, Type parameterType, bool preferQuery)
        {{
            throw new NotImplementedException(""Use any of Query, Load or Filter functions, or use GenericRepository class instead."");
        }}

        public global::{0}[] All()
        {{
            {1}
        }}

        public global::{0}[] Filter(FilterAll filterAll)
        {{
            return All();
        }}

",
                info.GetKeyProperties(),
                readFunctionBody);
        }

        private static string RepositoryQueryFunctionsSnippet(DataStructureInfo info, string queryFunctionBody)
        {
            return string.Format(
@"        public global::{0}.{1}[] Filter(IEnumerable<Guid> identifiers)
        {{
            const int BufferSize = 1000;
            int n = identifiers.Count();
            var result = new List<{0}.{1}>(n);
            for (int i = 0; i < (n+BufferSize-1) / BufferSize; i++) {{
                Guid[] idBuffer = identifiers.Skip(i*BufferSize).Take(BufferSize).ToArray();
                var itemBuffer = Query().Where(item => idBuffer.Contains(item.ID)).ToArray();
                result.AddRange(itemBuffer);
            }}
            return result.ToArray();
        }}

        public IQueryable<Common.Queryable.{0}_{1}> Query()
        {{
            {2}
            {3}
        }}

        public IQueryable<Common.Queryable.{0}_{1}> Query(object parameter, Type parameterType)
        {{
            throw new NotImplementedException(""Use the Query() function, or use GenericRepository class instead."");
        }}

",
                info.Module.Name,
                info.Name,
                BeforeQueryTag.Evaluate(info),
                queryFunctionBody);
        }

        public static void GenerateReadableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string readFunctionBody)
        {
            codeBuilder.InsertCode(RepositoryReadFunctionsSnippet(info, readFunctionBody), RepositoryMembers, info);
            codeBuilder.InsertCode("IReadableRepository<" + info.Module.Name + "." + info.Name + ">", RepositoryInterfaces, info);
        }

        public static void GenerateQueryableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string queryFunctionBody)
        {
            GenerateReadableRepositoryFunctions(info, codeBuilder, "return Query().ToArray();\r\n            ");
            codeBuilder.InsertCode(RepositoryQueryFunctionsSnippet(info, queryFunctionBody), RepositoryMembers, info);
            codeBuilder.InsertCode("IQueryableRepository<Common.Queryable." + info.Module.Name + "_" + info.Name + ">", RepositoryInterfaces, info);
            codeBuilder.InsertCode(SnippetQueryList(info), RepositoryMembers, info);
        }

        private static string SnippetQueryList(DataStructureInfo info)
        {
            string filterByIds = (info is BrowseDataStructureInfo || info is IOrmDataStructure)
                ? "Filter(Query(), ids)"
                : "Query().Where(item => ids.Contains(item.ID))";

            return string.Format(
@"        public IQueryable<Common.Queryable.{0}_{1}> QueryLoaded(IEnumerable<{0}.{1}> items)
        {{
            return items.Select(item => new Common.Queryable.{0}_{1}{4}
            {{
                ID = item.ID{2}
            }}).AsQueryable();
        }}

        public IQueryable<Common.Queryable.{0}_{1}> QueryPersisted(IEnumerable<{0}.{1}> items)
        {{
            var ids = items.Select(item => item.ID).ToList();
            return {3};
        }}

",
            info.Module.Name,
            info.Name,
            QueryLoadedAssignPropertyTag.Evaluate(info),
            filterByIds,
            QueryLoadedConstructorTag.Evaluate(info)); // {4}
        }
    }
}
