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
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RepositoryHelper
    {
        // Repository:
        public static readonly CsTag<DataStructureInfo> RepositoryAttributes = "RepositoryAttributes";
        public static readonly CsTag<DataStructureInfo> RepositoryInterfaces = new CsTag<DataStructureInfo>("RepositoryInterface", TagType.Appendable, ", {0}");
        public static readonly CsTag<DataStructureInfo> OverrideBaseTypeTag = new CsTag<DataStructureInfo>("OverrideBaseType", TagType.Reverse, " {0} //");
        public static readonly CsTag<DataStructureInfo> RepositoryPrivateMembers = "RepositoryPrivateMembers";
        public static readonly CsTag<DataStructureInfo> RepositoryMembers = "RepositoryMembers";
        public static readonly CsTag<DataStructureInfo> ConstructorArguments = "RepositoryConstructorArguments";
        public static readonly CsTag<DataStructureInfo> ConstructorCode = "RepositoryConstructorCode";

        // Readable repository:
        public static readonly CsTag<DataStructureInfo> BeforeQueryTag = "RepositoryBeforeQuery";

        // Queryable repository:
        public static readonly CsTag<DataStructureInfo> AssignSimplePropertyTag = "AssignSimpleProperty";

        public static void GenerateRepository(DataStructureInfo info, ICodeBuilder codeBuilder)
        {
            string module = info.Module.Name;
            string entity = info.Name;

            string repositorySnippet = $@"{RepositoryAttributes.Evaluate(info)}
    public class {entity}_Repository : {OverrideBaseTypeTag.Evaluate(info)} global::Common.RepositoryBase
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
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Extensibility.NamedPluginsExtensions));

            string registerRepository = $@"builder.RegisterType<{module}._Helper.{entity}_Repository>().Keyed<IRepository>(""{module}.{entity}"").InstancePerLifetimeScope();
            ";
            codeBuilder.InsertCode(registerRepository, ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
        
        public static void GenerateReadableRepository(DataStructureInfo info, ICodeBuilder codeBuilder, string loadFunctionBody)
        {
            GenerateRepository(info, codeBuilder);

            string module = info.Module.Name;
            string entity = info.Name;

            string repositoryReadFunctionsSnippet = $@"[Obsolete(""Use Load() or Query() method."")]
        public override global::{module}.{entity}[] All()
        {{
            {loadFunctionBody}
        }}

        ";
            codeBuilder.InsertCode(repositoryReadFunctionsSnippet, RepositoryMembers, info);
            codeBuilder.InsertCode($"Common.ReadableRepositoryBase<{module}.{entity}>", OverrideBaseTypeTag, info);
        }

        public static void GenerateQueryableRepository(DataStructureInfo info, ICodeBuilder codeBuilder, string queryFunctionBody, string loadFunctionBody = null)
        {
            GenerateReadableRepository(info, codeBuilder, loadFunctionBody ?? "return Query().ToSimple().ToArray();");

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
                codeBuilder.InsertCode($"Common.QueryableRepositoryBase<Common.Queryable.{module}_{entity}, {module}.{entity}>", OverrideBaseTypeTag, info);
            }

            string snippetToSimpleObjectsConversion = $@"/// <summary>Converts the objects with navigation properties to simple objects with primitive properties.</summary>
        public static IQueryable<{module}.{entity}> ToSimple(this IQueryable<Common.Queryable.{module}_{entity}> query)
        {{
            return query.Select(item => new {module}.{entity}
            {{
                ID = item.ID{AssignSimplePropertyTag.Evaluate(info)}
            }});
        }}
        ";
            codeBuilder.InsertCode(snippetToSimpleObjectsConversion, DomInitializationCodeGenerator.QueryExtensionsMembersTag);

            string snippetToSimpleObjectConversion = $@"/// <summary>Converts the object with navigation properties to a simple object with primitive properties.</summary>
        public {module}.{entity} ToSimple()
        {{
            var item = this;
            return new {module}.{entity}
            {{
                ID = item.ID{AssignSimplePropertyTag.Evaluate(info)}
            }};
        }}

        ";
            codeBuilder.InsertCode(snippetToSimpleObjectConversion, DataStructureQueryableCodeGenerator.MembersTag, info);

            string snippetToNavigationConversion = $@"/// <summary>Converts the simple object to a navigation object, and copies its simple properties. Navigation properties are set to null.</summary>
        public Common.Queryable.{module}_{entity} ToNavigation()
        {{
            var item = this;
            return new Common.Queryable.{module}_{entity}
            {{
                ID = item.ID{AssignSimplePropertyTag.Evaluate(info)}
            }};
        }}

        ";
            codeBuilder.InsertCode(snippetToNavigationConversion, DataStructureCodeGenerator.BodyTag, info);
        }
    }
}
