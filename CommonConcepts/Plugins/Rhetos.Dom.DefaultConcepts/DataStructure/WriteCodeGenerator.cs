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
@"        public void Save(IEnumerable<{0}> insertedNew, IEnumerable<{0}> updatedNew, IEnumerable<{0}> deletedIds, bool checkUserPermissions = false)
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

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (WriteInfo)conceptInfo;

            codeBuilder.InsertCode("IWritableRepository<" + info.DataStructure.Module.Name + "." + info.DataStructure.Name + ">", RepositoryHelper.RepositoryInterfaces, info.DataStructure);
            codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info.DataStructure);
        }
    }
}
