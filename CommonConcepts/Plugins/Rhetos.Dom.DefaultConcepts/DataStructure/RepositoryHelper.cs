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
        public static readonly CsTag<DataStructureInfo> RepositoryAttributes = "RepositoryAttributes";
        public static readonly CsTag<DataStructureInfo> RepositoryInterfaces = new CsTag<DataStructureInfo>("RepositoryInterface", TagType.Appendable, ", {0}");
        public static readonly CsTag<DataStructureInfo> OverrideBaseTypeTag = new CsTag<DataStructureInfo>("OverrideBaseType", TagType.Reverse, " {0} //");
        public static readonly CsTag<DataStructureInfo> RepositoryPrivateMembers = "RepositoryPrivateMembers";
        public static readonly CsTag<DataStructureInfo> RepositoryMembers = "RepositoryMembers";
        public static readonly CsTag<DataStructureInfo> AssignSimplePropertyTag = "AssignSimpleProperty";
        public static readonly CsTag<DataStructureInfo> ConstructorArguments = "RepositoryConstructorArguments";
        public static readonly CsTag<DataStructureInfo> ConstructorCode = "RepositoryConstructorCode";

        private static string RepositorySnippet(DataStructureInfo info)
        {
            return string.Format(
    RepositoryAttributes.Evaluate(info) + @"
    public class {0}_Repository : " + OverrideBaseTypeTag.Evaluate(info) + @" global::Common.RepositoryBase
        " + RepositoryInterfaces.Evaluate(info) + @"
    {{
        " + RepositoryPrivateMembers.Evaluate(info) + @"

        public {0}_Repository(Common.DomRepository domRepository, Common.ExecutionContext executionContext" + ConstructorArguments.Evaluate(info) + @")
        {{
            _domRepository = domRepository;
            _executionContext = executionContext;
            " + ConstructorCode.Evaluate(info) + @"
        }}

        " + RepositoryMembers.Evaluate(info) + @"
    }}

    ",
                info.Name);
        }

        private static string CallFromModuleRepostiorySnippet(DataStructureInfo info)
        {
            return string.Format(
        @"private {0}_Repository _{0}_Repository;
        public {0}_Repository {0} {{ get {{ return _{0}_Repository ?? (_{0}_Repository = ({0}_Repository)Rhetos.Extensibility.NamedPluginsExtensions.GetPlugin(_repositories, {1})); }} }}

        ",
                info.Name,
                CsUtility.QuotedString(info.Module.Name + "." + info.Name));
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
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Extensibility.NamedPluginsExtensions));
        }
        
        //==============================================================

        public static readonly CsTag<DataStructureInfo> BeforeQueryTag = "RepositoryBeforeQuery";

        private static string RepositoryReadFunctionsSnippet(DataStructureInfo info, string readFunctionBody)
        {
            return string.Format(
        @"[Obsolete(""Use Load() or Query() method."")]
        public override global::{0}[] All()
        {{
            {1}
        }}

        ",
                info.GetKeyProperties(),
                readFunctionBody);
        }

        public static void GenerateReadableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string loadFunctionBody)
        {
            codeBuilder.InsertCode(RepositoryReadFunctionsSnippet(info, loadFunctionBody), RepositoryMembers, info);
            codeBuilder.InsertCode("Common.ReadableRepositoryBase<" + info.Module.Name + "." + info.Name + ">", OverrideBaseTypeTag, info);
        }

        public static void GenerateQueryableRepositoryFunctions(DataStructureInfo info, ICodeBuilder codeBuilder, string queryFunctionBody, string loadFunctionBody = null)
        {
            string module = info.Module.Name;
            string entity = info.Name;

            if (loadFunctionBody == null)
                loadFunctionBody = "return Query().ToSimple().ToArray();";
            GenerateReadableRepositoryFunctions(info, codeBuilder, loadFunctionBody);

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

            string snippetToSimpleObjectsConversion = $@"case ""{module}.{entity}"":
                    return ((IQueryable<Common.Queryable.{module}_{entity}>)query).Select(item => new {module}.{entity}
                    {{
                        ID = item.ID{AssignSimplePropertyTag.Evaluate(info)}
                    }});
                ";
            codeBuilder.InsertCode(snippetToSimpleObjectsConversion, DomInitializationCodeGenerator.QueryableToSimpleTag);

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

            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.Graph));
        }
    }
}
