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
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(OrmDataStructureCodeGenerator))]
    public class ClaimRepositoryCodeGenerator : IConceptCodeGenerator
    {
        const string MemberFunctionsSnippet =
@"        public IList<ICommonClaim> LoadClaims()
        {
            return All();
        }

        public void SaveClaims(IList<Rhetos.Security.Claim> insert, IList<ICommonClaim> update, IList<ICommonClaim> delete)
        {
            Save(
                insert.Select(i => new Common.Claim { ClaimResource = i.Resource, ClaimRight = i.Right }).ToList(),
                update.Cast<Common.Claim>().ToList(),
                delete.Cast<Common.Claim>().ToList());
        }

";

        protected static string RegisterRepository(DataStructureInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().As<Rhetos.Dom.DefaultConcepts.IClaimRepository>();
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info.Module.Name == "Common" && info.Name == "Claim")
            {
                codeBuilder.InsertCode("Rhetos.Dom.DefaultConcepts.IClaimRepository", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.InsertCode(MemberFunctionsSnippet, RepositoryHelper.RepositoryMembers, info);
                codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.IClaimRepository));
            }
        }
    }
}
