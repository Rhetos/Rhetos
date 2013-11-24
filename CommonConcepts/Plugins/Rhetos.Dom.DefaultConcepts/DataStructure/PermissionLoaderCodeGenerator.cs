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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(BrowseDataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(BrowseDataStructureCodeGenerator))]
    public class PermissionLoaderCodeGenerator : IConceptCodeGenerator
    {
        const string MemberFunctionsSnippet =
@"        public IList<IPermission> LoadPermissions(IList<Rhetos.Security.Claim> claims, IList<string> principals)
        {
            var claimNames = claims.Select(claim => claim.Resource + ""."" + claim.Right).ToArray();
            return Query()
                .Where(permission => claimNames.Contains(permission.ClaimResource + ""."" + permission.ClaimRight) && principals.Contains(permission.Principal))
                .Cast<Rhetos.Dom.DefaultConcepts.IPermission>().ToList();
        }

";

        protected static string RegisterRepository(DataStructureInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().As<Rhetos.Dom.DefaultConcepts.IPermissionLoader>();
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info.Module.Name == "Common" && info.Name == "PermissionBrowse")
            {
                codeBuilder.InsertCode("Rhetos.Dom.DefaultConcepts.IPermissionLoader", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.InsertCode(MemberFunctionsSnippet, RepositoryHelper.RepositoryMembers, info);
                codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
                codeBuilder.AddReferencesFromDependency(typeof(IPermissionLoader));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Security.Claim));
            }
        }
    }
}
