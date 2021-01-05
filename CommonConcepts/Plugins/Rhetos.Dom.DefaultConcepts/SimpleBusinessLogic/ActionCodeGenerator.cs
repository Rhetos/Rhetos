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
    [ExportMetadata(MefProvider.Implements, typeof(ActionInfo))]
    public class ActionCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ActionInfo> BeforeActionTag = "BeforeAction";
        public static readonly CsTag<ActionInfo> AfterActionTag = "AfterAction";

        protected static string RepositoryFunctionsSnippet(ActionInfo info)
        {
            // Using nonstandard naming of variables to avoid name clashes with injected code.
            return string.Format(
        @"public void Execute({0}.{1} actionParameter)
        {{
            Action<{0}.{1}, Common.DomRepository, IUserInfo{3}> action_Object = {2};

            bool allEffectsCompleted = false;
            try
            {{
                {5}
                action_Object(actionParameter, _domRepository, _executionContext.UserInfo{4});
                {6}
                allEffectsCompleted = true;
            }}
            finally
            {{
                if (!allEffectsCompleted)
                    _executionContext.PersistenceTransaction.DiscardChanges();
            }}
        }}

        void IActionRepository.Execute(object actionParameter)
        {{
            Execute(({0}.{1}) actionParameter);
        }}

        ",
            info.Module.Name,
            info.Name,
            info.Script,
            DataStructureUtility.ComputationAdditionalParametersTypeTag.Evaluate(info),
            DataStructureUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info),
            BeforeActionTag.Evaluate(info),
            AfterActionTag.Evaluate(info));
        }

        public static string RegisterRepository(ActionInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IActionRepository>(""{0}.{1}"").InstancePerLifetimeScope();
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ActionInfo)conceptInfo;

            RepositoryHelper.GenerateRepository(info, codeBuilder);
            codeBuilder.InsertCode("IActionRepository", RepositoryHelper.RepositoryInterfaces, info);
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
    }
}
