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
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (WriteInfo)conceptInfo;

            codeBuilder.InsertCode("IWritableRepository<" + info.DataStructure.Module.Name + "." + info.DataStructure.Name + ">", RepositoryHelper.RepositoryInterfaces, info.DataStructure);
            codeBuilder.InsertCode("IValidateRepository", RepositoryHelper.RepositoryInterfaces, info.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(IWritableRepository<>));
            codeBuilder.AddReferencesFromDependency(typeof(IValidateRepository));

            codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.InvalidDataMessage));
        }

        protected static string MemberFunctionsSnippet(WriteInfo info)
        {
            return string.Format(
        @"public void Save(IEnumerable<{0}.{1}> insertedNew, IEnumerable<{0}.{1}> updatedNew, IEnumerable<{0}.{1}> deletedIds, bool checkUserPermissions = false)
        {{
            Rhetos.Utilities.CsUtility.Materialize(ref insertedNew);
            Rhetos.Utilities.CsUtility.Materialize(ref updatedNew);
            Rhetos.Utilities.CsUtility.Materialize(ref deletedIds);

            if (insertedNew == null) insertedNew = Enumerable.Empty<{0}.{1}>();
            if (updatedNew == null) updatedNew = Enumerable.Empty<{0}.{1}>();
            if (deletedIds == null) deletedIds = Enumerable.Empty<{0}.{1}>();

            if (insertedNew.Count() == 0 && updatedNew.Count() == 0 && deletedIds.Count() == 0)
                return;

            foreach (var item in insertedNew)
                if (item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

            " + WritableOrmDataStructureCodeGenerator.ClearContextTag.Evaluate(info.DataStructure) + @"

            " + WritableOrmDataStructureCodeGenerator.ArgumentValidationTag.Evaluate(info.DataStructure) + @"

            " + WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.DataStructure) + @"

            {2}

            bool allEffectsCompleted = false;
            try
            {{
                " + WritableOrmDataStructureCodeGenerator.OnSaveTag1.Evaluate(info.DataStructure) + @"

                " + WritableOrmDataStructureCodeGenerator.OnSaveTag2.Evaluate(info.DataStructure) + @"

                Rhetos.Dom.DefaultConcepts.InvalidDataMessage.ValidateOnSave(insertedNew, updatedNew, this, ""{0}.{1}"");
                allEffectsCompleted = true;
            }}
            finally
            {{
                if (!allEffectsCompleted)
                    _executionContext.PersistenceTransaction.DiscardChanges();
            }}
        }}

        public IEnumerable<Rhetos.Dom.DefaultConcepts.InvalidDataMessage> Validate(IList<Guid> ids, bool onSave)
        {{
            " + WritableOrmDataStructureCodeGenerator.OnSaveValidateTag.Evaluate(info.DataStructure) + @"
            yield break;
        }}

        ",
                info.DataStructure.Module.Name,
                info.DataStructure.Name,
                info.SaveImplementation);
        }
    }
}
