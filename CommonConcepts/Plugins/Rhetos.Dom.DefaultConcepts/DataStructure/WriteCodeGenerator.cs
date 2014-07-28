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
            if (insertedNew != null && !(insertedNew is System.Collections.IList)) insertedNew = insertedNew.ToList();
            if (updatedNew != null && !(updatedNew is System.Collections.IList)) updatedNew = updatedNew.ToList();
            if (deletedIds != null && !(deletedIds is System.Collections.IList)) deletedIds = deletedIds.ToList();

            if (insertedNew == null) insertedNew = new {0}[] {{ }};
            if (updatedNew == null) updatedNew = new {0}[] {{ }};
            if (deletedIds == null) deletedIds = new {0}[] {{ }};

            if (insertedNew.Count() == 0 && updatedNew.Count() == 0 && deletedIds.Count() == 0)
                return;

            foreach (var item in insertedNew)
                if (item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

            {5}

            {2}

            {1}

            {3}

            {4}
        }}

",
                info.DataStructure.GetKeyProperties(),
                info.SaveImplementation,
                WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.DataStructure),
                WritableOrmDataStructureCodeGenerator.OnSaveTag1.Evaluate(info.DataStructure),
                WritableOrmDataStructureCodeGenerator.OnSaveTag2.Evaluate(info.DataStructure),
                WritableOrmDataStructureCodeGenerator.ArgumentValidationTag.Evaluate(info.DataStructure));
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
