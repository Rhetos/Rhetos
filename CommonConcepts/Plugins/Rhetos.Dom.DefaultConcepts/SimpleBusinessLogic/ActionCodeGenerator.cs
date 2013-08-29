﻿/*
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ActionInfo))]
    public class ActionCodeGenerator : IConceptCodeGenerator
    {
        protected static string RepositoryFunctionsSnippet(ActionInfo info)
        {
            return string.Format(
@"        private static readonly Action<{0}.{1}, Common.DomRepository, IUserInfo{3}> _action = {2};

        public void Execute({0}.{1} parameters)
        {{
            _action(parameters, _domRepository, _executionContext.UserInfo{4});
        }}

        void IActionRepository.Execute(object parameters)
        {{
            Execute(({0}.{1}) parameters);
        }}

",
             info.Module.Name,
             info.Name,
             info.Script,
             DomUtility.ComputationAdditionalParametersTypeTag.Evaluate(info),
             DomUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info));
        }

        public static string RegisterRepository(ActionInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IActionRepository>(""{0}.{1}"");
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ActionInfo)conceptInfo;

            RepositoryHelper.GenerateRepository(info, codeBuilder);
            codeBuilder.InsertCode("IActionRepository", RepositoryHelper.RepositoryInterfaces, info);
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);

            codeBuilder.AddReferencesFromDependency(typeof(ExportAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(ExportMetadataAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(MefProvider));
        }
    }
}
