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

using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RepositoryHelper
    {
        // Repository tags:
        public static readonly CsTag<DataStructureInfo> RepositoryAttributes = "RepositoryAttributes";
        public static readonly CsTag<DataStructureInfo> RepositoryInterfaces = new CsTag<DataStructureInfo>("RepositoryInterface", TagType.Appendable, ", {0}");
        public static readonly CsTag<DataStructureInfo> OverrideBaseTypeTag = new CsTag<DataStructureInfo>("OverrideBaseType", TagType.Reverse, " {0} //");
        public static readonly CsTag<DataStructureInfo> RepositoryPrivateMembers = "RepositoryPrivateMembers";
        public static readonly CsTag<DataStructureInfo> RepositoryMembers = "RepositoryMembers";
        public static readonly CsTag<DataStructureInfo> ConstructorArguments = "RepositoryConstructorArguments";
        public static readonly CsTag<DataStructureInfo> ConstructorCode = "RepositoryConstructorCode";

        /// <summary>
        /// KeyValuePair inserted here should have the key string set to the type name as written in C# source code (with or without namespace),
        /// often used as a filter name.
        /// <see cref="IDataStructureReadParameters"/> will provide this data the other components, and optionally extended the
        /// type names to including type names with removed default namespaces and also type names as returned by Type.ToString.
        /// </summary>
        public static readonly CsTag<DataStructureInfo> ReadParameterTypesTag = "ReadParameterTypes";

        // Readable repository tags:
        public static readonly CsTag<DataStructureInfo> BeforeQueryTag = "RepositoryBeforeQuery";

        // Queryable repository tags:
        public static readonly CsTag<DataStructureInfo> AssignSimplePropertyTag = "AssignSimpleProperty";

        public static void GenerateRepository(DataStructureInfo info, ICodeBuilder codeBuilder)
        {
            string module = info.Module.Name;
            string entity = info.Name;

            string repositorySnippet = $@"{RepositoryAttributes.Evaluate(info)}
    public partial class {entity}_Repository : {OverrideBaseTypeTag.Evaluate(info)} global::Common.RepositoryBase
        {RepositoryInterfaces.Evaluate(info)}
    {{
        {RepositoryPrivateMembers.Evaluate(info)}

        public {entity}_Repository(Common.DomRepository domRepository, Common.ExecutionContext executionContext{ConstructorArguments.Evaluate(info)})
        {{
            _domRepository = domRepository;
            _executionContext = executionContext;
            {ConstructorCode.Evaluate(info)}
        }}

        {RepositoryMembers.Evaluate(info)}
    }}

    ";
            codeBuilder.InsertCode(repositorySnippet, ModuleCodeGenerator.HelperNamespaceMembersTag, info.Module);

            string callFromModuleRepostiorySnippet = $@"private {entity}_Repository _{entity}_Repository;
        public {entity}_Repository {entity} {{ get {{ return _{entity}_Repository ?? (_{entity}_Repository = ({entity}_Repository)Rhetos.Extensibility.NamedPluginsExtensions.GetPlugin(_repositories, {CsUtility.QuotedString(module + "." + entity)})); }} }}

        ";
            codeBuilder.InsertCode(callFromModuleRepostiorySnippet, ModuleCodeGenerator.RepositoryMembersTag, info.Module);

            string registerRepository = $@"builder.RegisterType<{module}.Repositories.{entity}_Repository>().Keyed<IRepository>(""{module}.{entity}"").InstancePerLifetimeScope();
            ";
            codeBuilder.InsertCode(registerRepository, ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
        
        public static void GenerateReadableRepository(DataStructureInfo info, ICodeBuilder codeBuilder, string loadFunctionBody = null)
        {
            GenerateRepository(info, codeBuilder);

            string module = info.Module.Name;
            string entity = info.Name;

            if (loadFunctionBody != null)
            {
                string repositoryReadFunctionsSnippet = $@"public override global::{module}.{entity}[] Load()
        {{
            {loadFunctionBody}
        }}

        ";
                codeBuilder.InsertCode(repositoryReadFunctionsSnippet, RepositoryMembers, info);
            }

            codeBuilder.InsertCode($"Common.ReadableRepositoryBase<{module}.{entity}>", OverrideBaseTypeTag, info);

            codeBuilder.InsertCode(
        $@"public static KeyValuePair<string, Type>[] GetReadParameterTypes()
        {{
            return new KeyValuePair<string, Type>[]
            {{
                {ReadParameterTypesTag.Evaluate(info)}
            }};
        }}
        
        ",
                RepositoryMembers, info);

            codeBuilder.InsertCode(
            $@"{{ ""{module}.{entity}"", {module}.Repositories.{entity}_Repository.GetReadParameterTypes }},
            ",
                ModuleCodeGenerator.DataStructuresReadParameterTypesTag);
        }

        public static void GenerateQueryableRepository(DataStructureInfo info, ICodeBuilder codeBuilder, string queryFunctionBody = null, string loadFunctionBody = null)
        {
            GenerateReadableRepository(info, codeBuilder, loadFunctionBody);

            string module = info.Module.Name;
            string entity = info.Name;

            if (queryFunctionBody != null)
            {
                string repositoryQueryFunctionsSnippet =
        $@"public override IQueryable<Common.Queryable.{module}_{entity}> Query()
        {{
            {BeforeQueryTag.Evaluate(info)}
            {queryFunctionBody}
        }}

        ";
                codeBuilder.InsertCode(repositoryQueryFunctionsSnippet, RepositoryMembers, info);
            }

            codeBuilder.InsertCode($"Common.QueryableRepositoryBase<Common.Queryable.{module}_{entity}, {module}.{entity}>", OverrideBaseTypeTag, info);
        }
    }
}
