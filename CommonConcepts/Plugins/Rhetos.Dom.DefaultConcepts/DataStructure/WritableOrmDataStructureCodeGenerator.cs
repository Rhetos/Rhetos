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
    public class WritableOrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        /// <summary>Clear objects and context from any state other than new values to be saved.</summary>
        public static readonly CsTag<DataStructureInfo> ClearContextTag = "WritableOrm ClearContext";

        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds".</summary>
        public static readonly CsTag<DataStructureInfo> ArgumentValidationTag = "WritableOrm ArgumentValidation";

        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds".</summary>
        public static readonly CsTag<DataStructureInfo> InitializationTag = "WritableOrm Initialization";

        /// <summary>Lists "updated" and "deleted" contain OLD data.
        /// Enumerables "insertedNew", "updatedNew" and "deletedIds" contain NEW data.
        /// Sample usage: 1. Verify that locked items are not going to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> OldDataLoadedTag = "WritableOrm OldDataLoaded";

        /// <summary>Use <see cref="OldDataLoadedTag"/> instead of this tag, unless you need to make additional post-processing.
        /// Lists "updated" and "deleted" contain OLD data.
        /// Enumerables "insertedNew", "updatedNew" and "deletedIds" contain NEW data.
        /// Sample usage: 1. Verify that locked items are not going to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> ProcessedOldDataTag = "WritableOrm ProcessedOldData";

        /// <summary>Insert code here to recompute (insert/update/delete) other entities that depend on the changes items.
        /// Queries "inserted" and "updated" will return NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag1 = "WritableOrm OnSaveTag1";

        /// <summary>Insert code here to verify that invalid items are not going to be inserted or updated.
        /// Queries "inserted" and "updated" will return NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag2 = "WritableOrm OnSaveTag2";

        /// <summary>Insert code here to returns a list at errors for the given items (IList&lt;Guid&gt; ids).
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveValidateTag = "WritableOrm OnSaveValidate";

        /// <summary>
        /// Entity-specific interpretation of database errors.
        /// </summary>
        public static readonly CsTag<DataStructureInfo> OnDatabaseErrorTag = "WritableOrm OnDatabaseError";

        /// <summary>The inserted code will be execute after recomputing and validations.
        /// Queries "inserted" and "updated" will return NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> AfterSaveTag = "WritableOrm AfterSave";

        public static readonly CsTag<DataStructureInfo> PersistenceStorageMapperPropertyMappingTag = "PersistenceStorageMapperPropertyMapping";

        public static readonly CsTag<DataStructureInfo> PersistenceStorageMapperDependencyResolutionTag = "PersistenceStorageMapperDependencyResolution";

        protected static string MemberFunctionsSnippet(DataStructureInfo info)
        {
            return string.Format(
        @"public virtual void Save(IEnumerable<{0}.{1}> insertedNew, IEnumerable<{0}.{1}> updatedNew, IEnumerable<{0}.{1}> deletedIds, bool checkUserPermissions = false)
        {{
            if (!DomHelper.InitializeSaveMethodItems(ref insertedNew, ref updatedNew, ref deletedIds))
                return;

            " + ClearContextTag.Evaluate(info) + @"

            " + ArgumentValidationTag.Evaluate(info) + @"

            " + InitializationTag.Evaluate(info) + @"

            // Using old data, including lazy loading of navigation properties:

            IEnumerable<Common.Queryable.{0}_{1}> deleted = DomHelper.LoadOldDataWithNavigationProperties(deletedIds, this);
            IEnumerable<Common.Queryable.{0}_{1}> updated = DomHelper.LoadOldDataWithNavigationProperties(updatedNew, this);

            " + OldDataLoadedTag.Evaluate(info) + @"

            " + ProcessedOldDataTag.Evaluate(info) + @"

            {{
                DomHelper.WriteToDatabase(insertedNew, updatedNew, deletedIds, _executionContext.PersistenceStorage, checkUserPermissions, _sqlUtility,
                    out Exception saveException, out Rhetos.RhetosException interpretedException);

                if (saveException != null)
                {{
                    " + OnDatabaseErrorTag.Evaluate(info) + @"
                    DomHelper.ThrowInterpretedException(checkUserPermissions, saveException, interpretedException, _sqlUtility, ""{0}.{1}"");
                }}
            }}

            deleted = null;
            updated = this.Query(updatedNew.Select(item => item.ID));
            IEnumerable<Common.Queryable.{0}_{1}> inserted = this.Query(insertedNew.Select(item => item.ID));

            bool allEffectsCompleted = false;
            try
            {{
                " + OnSaveTag1.Evaluate(info) + @"

                " + OnSaveTag2.Evaluate(info) + @"

                Rhetos.Dom.DefaultConcepts.InvalidDataMessage.ValidateOnSave(insertedNew, updatedNew, this, ""{0}.{1}"");

                " + AfterSaveTag.Evaluate(info) + @"

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
            " + OnSaveValidateTag.Evaluate(info) + @"
            yield break;
        }}

        ",
                info.Module.Name,
                info.Name);
        }

        protected static string PersistenceStorageMappingSnippet(DataStructureInfo info)
        {
            return
    $@"public class {info.Module.Name}_{info.Name}_Mapper : IPersistenceStorageObjectMapper
    {{
        public PersistenceStorageObjectParameter[] GetParameters(IEntity genericEntity)
        {{
            var entity = ({info.Module}.{info.Name})genericEntity;
            return new PersistenceStorageObjectParameter[]
            {{
                new PersistenceStorageObjectParameter(""ID"", new SqlParameter("""", entity.ID)),
                {PersistenceStorageMapperPropertyMappingTag.Evaluate(info)}
            }};
        }}
    
    	public IEnumerable<Guid> GetDependencies(IEntity genericEntity)
        {{
            var entity = ({info.Module}.{info.Name})genericEntity;
            {PersistenceStorageMapperDependencyResolutionTag.Evaluate(info)}
            yield break;
        }}
    
    	public string GetTableName()
        {{
            return ""{(info as IOrmDataStructure).GetOrmSchema()}.{(info as IOrmDataStructure).GetOrmDatabaseObject()}"";
        }}
    }}

    ";
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode("IWritableRepository<" + info.Module.Name + "." + info.Name + ">", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.InsertCode("IValidateRepository", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.AddReferencesFromDependency(typeof(IWritableRepository<>));
                codeBuilder.AddReferencesFromDependency(typeof(IValidateRepository));

                codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ExceptionsUtility));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.CsUtility));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.InvalidDataMessage));

                codeBuilder.InsertCode(PersistenceStorageMappingSnippet(info), DomInitializationCodeGenerator.PersistenceStorageMappingsTag);
                string registerRepository = $@"_mappings.Add(typeof({info.Module.Name}.{info.Name}), new {info.Module.Name}_{info.Name}_Mapper());
            ";
                codeBuilder.InsertCode(registerRepository, DomInitializationCodeGenerator.PersistenceStorageMappingRegistrationTag);
            }
        }
    }
}
