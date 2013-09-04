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
    [ExportMetadata(MefProvider.Implements, typeof(WriteInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(OrmDataStructureCodeGenerator))]
    public class WriteCodeGenerator : IConceptCodeGenerator
    {

        protected static string MemberFunctionsSnippet(WriteInfo info)
        {
            return string.Format(
@"        void Rhetos.Dom.DefaultConcepts.IWritableRepository.Save(IEnumerable<object> insertedNew, IEnumerable<object> updatedNew, IEnumerable<object> deletedIds, bool checkUserPermissions = false)
        {{
            Save(
                insertedNew != null ? insertedNew.Cast<{0}>() : null,
                updatedNew != null ? updatedNew.Cast<{0}>() : null,
                deletedIds != null ? deletedIds.Cast<{0}>() : null,
                checkUserPermissions);
        }}

        public void Insert(IEnumerable<object> items)
        {{
            Save(items.Cast<{0}>(), null, null);
        }}

        public void Update(IEnumerable<object> items)
        {{
            Save(null, items.Cast<{0}>(), null);
        }}

        public void Delete(IEnumerable<object> items)
        {{
            Save(null, null, items.Cast<{0}>());
        }}

        public void Save(IEnumerable<{0}> insertedNew, IEnumerable<{0}> updatedNew, IEnumerable<{0}> deletedIds, bool checkUserPermissions = false)
        {{
            {1}
        }}

",
                info.DataStructure.GetKeyProperties(),
                info.SaveImplementation);
        }

        protected static string RegisterRepository(DataStructureInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IWritableRepository>(""{0}.{1}"");
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (WriteInfo)conceptInfo;

            codeBuilder.InsertCode("IWritableRepository", RepositoryHelper.RepositoryInterfaces, info.DataStructure);
            codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info.DataStructure);
            codeBuilder.InsertCode(RegisterRepository(info.DataStructure), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }
    }
}
