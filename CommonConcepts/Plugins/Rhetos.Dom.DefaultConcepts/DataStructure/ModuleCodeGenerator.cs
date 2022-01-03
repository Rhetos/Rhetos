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
using System.IO;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ModuleInfo))]
    public class ModuleCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ModuleInfo> RepositoryMembersTag = "RepositoryMembers";
        private readonly CommonConceptsOptions _commonConceptsOptions;

        public ModuleCodeGenerator(CommonConceptsOptions commonConceptsOptions)
        {
            _commonConceptsOptions = commonConceptsOptions;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ModuleInfo)conceptInfo;

            string moduleRepositorySnippet =
$@"namespace {info.Name}._Helper
{{
    {DomInitializationCodeGenerator.DisableWarnings(_commonConceptsOptions)}{DomInitializationCodeGenerator.StandardNamespacesSnippet}

    public class _ModuleRepository
    {{
        private readonly Rhetos.Extensibility.INamedPlugins<IRepository> _repositories;

        public _ModuleRepository(Rhetos.Extensibility.INamedPlugins<IRepository> repositories)
        {{
            _repositories = repositories;
        }}

        {RepositoryMembersTag.Evaluate(info)}
    }}{DomInitializationCodeGenerator.RestoreWarnings(_commonConceptsOptions)}
}}
";
            string moduleRepositoryFile = $"{Path.Combine(GeneratedSourceDirectories.Repositories.ToString(), info.Name + "ModuleRepository")}";

            codeBuilder.InsertCodeToFile(moduleRepositorySnippet, moduleRepositoryFile);

            codeBuilder.InsertCode($@"private {info.Name}._Helper._ModuleRepository _{info.Name};
        public {info.Name}._Helper._ModuleRepository {info.Name} {{ get {{ return _{info.Name} ?? (_{info.Name} = new {info.Name}._Helper._ModuleRepository(_repositories)); }} }}

        ", CommonDomRepositoryMembersTag);
        }

        // TODO: Move this tags to DomInitializationCodeGenerator (breaking backward compatibility for other DSL packages)
        public const string CommonUsingTag = "/*CommonUsing*/";
        public const string CommonDomRepositoryMembersTag = "/*CommonDomRepositoryMembers*/";
        public const string CommonAutofacConfigurationMembersTag = "/*CommonAutofacConfigurationMembers*/";
        public const string ExecutionContextMemberTag = "/*ExecutionContextMember*/";
        public const string ExecutionContextConstructorArgumentTag = "/*ExecutionContextConstructorArgument*/";
        public const string ExecutionContextConstructorAssignmentTag = "/*ExecutionContextConstructorAssignment*/";
        public const string RegisteredInterfaceImplementationNameTag = "/*RegisteredInterfaceImplementationName*/";
        public const string ApplyFiltersOnClientReadTag = "/*ApplyFiltersOnClientRead*/";
        public const string CommonNamespaceMembersTag = "/*CommonNamespaceMembers*/";
        public const string CommonInfrastructureMembersTag = "/*CommonInfrastructureMembers*/";
        public const string DataStructuresReadParameterTypesTag = "/*DataStructuresReadParameterTypes*/";
    }
}
