/*
    Copyright (C) 2013 Omega software d.o.o.

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
        public static readonly CsTag<DataStructureInfo> RepositoryInterfaces = new CsTag<DataStructureInfo>("RepositoryInterface", TagType.Appendable, ":\r\n        {0}", ",\r\n        {0}");
        public static readonly CsTag<DataStructureInfo> RepositoryMembers = "RepositoryMembers";

        private static string RepositorySnippet(DataStructureInfo info)
        {
            return string.Format(
@"{1}
    public class {0}_Repository {2}
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

        public static void GenerateRepository(DataStructureInfo info, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(RepositorySnippet(info), ModuleCodeGenerator.HelperNamespaceMembersTag, info.Module);
            codeBuilder.InsertCode(CallFromModuleRepostiorySnippet(info), ModuleCodeGenerator.RepositoryMembersTag, info.Module);
        }
        
        //==============================================================

        public static readonly CsTag<DataStructureInfo> BeforeQueryTag = "RepositoryBeforeQuery";

        private static string RepositoryReadFunctionsSnippet(DataStructureInfo info, string readFunctionBody)
        {
            return string.Format(
@"        public global::{0}[] All()
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
@"        public global::{0}[] Filter(IEnumerable<Guid> identifiers)
        {{
			const int BufferSize = 1000;
			int n = identifiers.Count();
			var result = new List<{0}>(n);
			for (int i = 0; i < (n+BufferSize-1) / BufferSize; i++) {{
				Guid[] idBuffer = identifiers.Skip(i*BufferSize).Take(BufferSize).ToArray();
				var itemBuffer = Query().Where(item => idBuffer.Contains(item.ID)).ToArray();
				result.AddRange(itemBuffer);
			}}
            return result.ToArray();
        }}

        public IQueryable<global::{0}> Query()
        {{
            {1}
            {2}
        }}

        Rhetos.Processing.DefaultCommands.QueryDataSourceCommandResult Rhetos.Processing.DefaultCommands.IQueryDataSourceCommandImplementation.QueryData(Rhetos.Processing.DefaultCommands.QueryDataSourceCommandInfo commandInfo)
        {{
            var repository = _domRepository.{0};

            {0}[] filteredResult = null;
            if (commandInfo.Filter != null)
            {{
                // Using reflection to execute function 'repository.Filter(commandInfo.Filter)'. The function is an implementation of the interface IFilterRepository<TFilter, TResult>.
                Type filterRepositoryType = repository.GetType().FindInterfaces(System.Reflection.Module.FilterTypeName, typeof(IFilterRepository<,>).Name)
                    .Where(t => t.GetGenericArguments().First().IsAssignableFrom(commandInfo.Filter.GetType())).SingleOrDefault();
                if (filterRepositoryType != null)
                {{
                    System.Reflection.MethodInfo filterMethod = filterRepositoryType.GetMethod(""Filter"", new[] {{ commandInfo.Filter.GetType() }});
                    filteredResult = ({0}[]) filterMethod.Invoke(repository, new[] {{ commandInfo.Filter }});
                }}
                else
                    throw new Rhetos.FrameworkException(""Data stucture '{0}' does not have an implementation of the filter for type '"" + commandInfo.Filter.GetType().FullName + ""'."");
            }}

            IQueryable<{0}> query = filteredResult != null ? filteredResult.AsQueryable() : _domRepository.{0}.Query();

            if (commandInfo.GenericFilter != null)
                query = Rhetos.Dom.DefaultConcepts.GenericFilterWithPagingUtility.Filter(query, commandInfo.GenericFilter);

            int totalCount = -1;
            query = Rhetos.Dom.DefaultConcepts.GenericFilterWithPagingUtility.SortAndPaginate(query, commandInfo, ref totalCount);

            var result = new Rhetos.Processing.DefaultCommands.QueryDataSourceCommandResult
            {{
                Records = query.ToArray(),
                TotalRecords = totalCount
            }};

            if (result.TotalRecords == -1)
                result.TotalRecords = result.Records.Count();

            return result;
        }}

",
                info.GetKeyProperties(),
                BeforeQueryTag.Evaluate(info),
                queryFunctionBody);
        }

        private static string RegisterQueryDataSourceCommandImplementation(DataStructureInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<Rhetos.Processing.DefaultCommands.IQueryDataSourceCommandImplementation>(""{0}.{1}"");
            ",
                info.Module.Name, info.Name);
        }

        public static void GenerateReadableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string readFunctionBody)
        {
            codeBuilder.InsertCode(RepositoryReadFunctionsSnippet(info, readFunctionBody), RepositoryMembers, info);
            codeBuilder.InsertCode("IFilterRepository<FilterAll, " + info.Module.Name + "." + info.Name + ">", RepositoryInterfaces, info);
        }

        public static void GenerateQueryableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string queryFunctionBody)
        {
            GenerateReadableRepositoryFunctions(info, codeBuilder, "return Query().ToArray();\r\n            ");
            codeBuilder.InsertCode(RepositoryQueryFunctionsSnippet(info, queryFunctionBody), RepositoryMembers, info);
            codeBuilder.InsertCode("IFilterRepository<IEnumerable<Guid>, " + info.Module.Name + "." + info.Name + ">", RepositoryInterfaces, info);
            codeBuilder.InsertCode("Rhetos.Processing.DefaultCommands.IQueryDataSourceCommandImplementation", RepositoryInterfaces, info);
            codeBuilder.InsertCode(RegisterQueryDataSourceCommandImplementation(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);

            codeBuilder.AddReferencesFromDependency(typeof(IQueryDataSourceCommandImplementation));
            codeBuilder.AddReferencesFromDependency(typeof(ICommandInfo));
            codeBuilder.AddReferencesFromDependency(typeof(GenericFilterWithPagingUtility));
            codeBuilder.AddReferencesFromDependency(typeof(QueryDataSourceCommandResult));
        }
    }
}
