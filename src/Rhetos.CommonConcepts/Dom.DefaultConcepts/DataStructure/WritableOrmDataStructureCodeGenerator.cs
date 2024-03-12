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
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(OrmDataStructureCodeGenerator))]
    public class WritableOrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        /// <summary>Clear objects and context from any state other than new values to be saved.</summary>
        public static readonly CsTag<DataStructureInfo> ClearContextTag = "WritableOrm ClearContext";

        /// <summary>Inserted code can use lists "insertedNew", "updatedNew" and "deletedIds".</summary>
        public static readonly CsTag<DataStructureInfo> ArgumentValidationTag = "WritableOrm ArgumentValidation";

        /// <summary>Inserted code can use lists "insertedNew", "updatedNew" and "deletedIds".</summary>
        public static readonly CsTag<DataStructureInfo> InitializationTag = "WritableOrm Initialization";

        /// <summary>Lists "updated" and "deleted" contain the OLD data.
        /// Lists "insertedNew", "updatedNew" and "deletedIds" contain NEW data.
        /// Sample usage: 1. Verify that locked items are not going to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> OldDataLoadedTag = "WritableOrm OldDataLoaded";

        /// <summary>Use <see cref="OldDataLoadedTag"/> instead of this tag, unless you need to make additional post-processing.
        /// Lists "updated" and "deleted" contain the OLD data.
        /// Lists "insertedNew", "updatedNew" and "deletedIds" contain NEW data.
        /// Sample usage: 1. Verify that locked items are not going to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> ProcessedOldDataTag = "WritableOrm ProcessedOldData";

        /// <summary>Insert code here to recompute (insert/update/delete) other entities that depend on the changes items.
        /// Lists "inserted" and "updated" contain the NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag1 = "WritableOrm OnSaveTag1";

        /// <summary>Insert code here to verify that invalid items are not going to be inserted or updated.
        /// Lists "inserted" and "updated" contain the NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag2 = "WritableOrm OnSaveTag2";

        /// <summary>Insert code here to returns a list at errors for the given items (IList&lt;Guid&gt; ids).
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveValidateTag = "WritableOrm OnSaveValidate";

        /// <summary>
        /// Entity-specific interpretation of database errors.
        /// </summary>
        public static readonly CsTag<DataStructureInfo> ErrorMetadataTag = "WritableOrm GetErrorMetadata";

        /// <summary>The inserted code will be execute after recomputing and validations.
        /// Lists "inserted" and "updated" contain the NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been committed).</summary>
        public static readonly CsTag<DataStructureInfo> AfterSaveTag = "WritableOrm AfterSave";

        public static readonly CsTag<DataStructureInfo> PersistenceStorageMapperPropertyMappingTag = "PersistenceStorageMapperPropertyMapping";

        public static readonly CsTag<DataStructureInfo> PersistenceStorageMapperDependencyResolutionTag = "PersistenceStorageMapperDependencyResolution";

        private readonly ConceptMetadata _conceptMetadata;
        private readonly ISqlResources _sqlResources;

        public WritableOrmDataStructureCodeGenerator(ConceptMetadata conceptMetadata, ISqlResources sqlResources)
        {
            _conceptMetadata = conceptMetadata;
            _sqlResources = sqlResources;
        }

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

            IEnumerable<{0}.{1}> deleted = DomHelper.LazyLoadData(deletedIds, this);
            IEnumerable<{0}.{1}> updated = DomHelper.LazyLoadData(updatedNew, this);

            " + OldDataLoadedTag.Evaluate(info) + @"

            " + ProcessedOldDataTag.Evaluate(info) + @"

            {{
                static string GetErrorMetadata(string tableName, string constraintName) => (tableName, constraintName) switch
                {{
                    " + ErrorMetadataTag.Evaluate(info) + @" _ => null
                }};
                DomHelper.WriteToDatabase(insertedNew, updatedNew, deletedIds, _executionContext.PersistenceStorage, checkUserPermissions, _executionContext.SqlUtility, GetErrorMetadata);
            }}

            deleted = null;
            updated = updatedNew;
            IEnumerable<{0}.{1}> inserted = insertedNew;

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
                    _executionContext.PersistenceTransaction.DiscardOnDispose();
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

        protected string PersistenceStorageMappingSnippet(DataStructureInfo info)
        {
            string dbParameterClass = _sqlResources.Get("DbParameterClass");
            string dbParameterType = _sqlResources.Get("StorageMappingDbType_Guid");

            return
    $@"public class {info.Module.Name}_{info.Name}_Mapper : IPersistenceStorageObjectMapper
    {{
        public PersistenceStorageObjectParameter[] GetParameters(IEntity genericEntity)
        {{
            var entity = ({info.Module}.{info.Name})genericEntity;
            return new PersistenceStorageObjectParameter[]
            {{
                new PersistenceStorageObjectParameter(""ID"", new {dbParameterClass}("""", {dbParameterType}) {{ Value = entity.ID }}),
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
            return ""{_conceptMetadata.GetOrmSchema(info)}.{_conceptMetadata.GetOrmDatabaseObject(info)}"";
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

                codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);

                codeBuilder.InsertCode(PersistenceStorageMappingSnippet(info), DomInitializationCodeGenerator.PersistenceStorageMappingsTag);
                string registerRepository = $@"_mappings.Add(typeof({info.Module.Name}.{info.Name}), new {info.Module.Name}_{info.Name}_Mapper());
            ";
                codeBuilder.InsertCode(registerRepository, DomInitializationCodeGenerator.PersistenceStorageMappingRegistrationTag);
            }
        }
    }
}
