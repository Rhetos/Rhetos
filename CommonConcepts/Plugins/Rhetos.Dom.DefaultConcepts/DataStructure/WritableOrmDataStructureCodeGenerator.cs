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
        /// Sample usage: 1. Verify that locked items are not goind to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> OldDataLoadedTag = "WritableOrm OldDataLoaded";

        [Obsolete]
        public static readonly CsTag<DataStructureInfo> ProcessedOldDataTag = "WritableOrm ProcessedOldData";

        /// <summary>Insert code here to recompute (insert/update/delete) other entities that depend on the changes items.
        /// Queries "inserted" and "updated" will return NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations can be used.</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag1 = "WritableOrm OnSaveTag1";

        /// <summary>Insert code here to verify that invalid items are not going to be inserted or updated.
        /// Queries "inserted" and "updated" will return NEW data.
        /// Data is already saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations can be used.</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag2 = "WritableOrm OnSaveTag2";

        // TODO: Remove "duplicateObjects" check after implementing stateless session with "manual" saving.
        protected static string MemberFunctionsSnippet(DataStructureInfo info)
        {
            return string.Format(
@"        public void Save(IEnumerable<{0}.{1}> insertedNew, IEnumerable<{0}.{1}> updatedNew, IEnumerable<{0}.{1}> deletedIds, bool checkUserPermissions = false)
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

            " + ClearContextTag.Evaluate(info) + @"

            _executionContext.EntityFrameworkContext.ClearCache(); // Updating a modified persistent object could break old-data validations such as checking for locked items.

            if (insertedNew.Count() > 0)
            {{
                var duplicateObjects = Filter(insertedNew.Select(item => item.ID).ToArray());
                if (duplicateObjects.Count() > 0)
                {{
                    var deletedIndex = new HashSet<Guid>(deletedIds.Select(item => item.ID));
                    duplicateObjects = duplicateObjects.Where(item => !deletedIndex.Contains(item.ID)).ToArray();
                }}
                if (duplicateObjects.Count() > 0)
                {{
                    string msg = ""Inserting a record that already exists. ID="" + duplicateObjects.First().ID;
                    if (checkUserPermissions)
                        throw new Rhetos.ClientException(msg);
                    else
                        throw new Rhetos.FrameworkException(msg);
                }}
            }}

            " + ArgumentValidationTag.Evaluate(info) + @"

            " + InitializationTag.Evaluate(info) + @"

            // Using old data:
            IEnumerable<Common.Queryable.{0}_{1}> deleted = LoadPersistedWithReferences(deletedIds).ToList();
            IEnumerable<Common.Queryable.{0}_{1}> updated = LoadPersistedWithReferences(updatedNew).ToList();

            " + OldDataLoadedTag.Evaluate(info) + @"

            " + ProcessedOldDataTag.Evaluate(info) + @"

            deleted = QueryLoaded(deletedIds);
            updated = QueryLoaded(updatedNew);
            IEnumerable<Common.Queryable.{0}_{1}> inserted = QueryLoaded(insertedNew);

            try
            {{
                if (deletedIds.Count() > 0)
                {{
                    _executionContext.EntityFrameworkContext.ClearCache();
                    _executionContext.EntityFrameworkContext.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in deleted)
                        _executionContext.EntityFrameworkContext.Entry(item).State = System.Data.Entity.EntityState.Deleted;
                    _executionContext.EntityFrameworkContext.Configuration.AutoDetectChangesEnabled = true;
                    _executionContext.EntityFrameworkContext.SaveChanges();
                }}

                if (updatedNew.Count() > 0)
                {{
                    _executionContext.EntityFrameworkContext.ClearCache();
                    _executionContext.EntityFrameworkContext.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in updated)
                        _executionContext.EntityFrameworkContext.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    _executionContext.EntityFrameworkContext.Configuration.AutoDetectChangesEnabled = true;
                    _executionContext.EntityFrameworkContext.SaveChanges();
                }}

                if (insertedNew.Count() > 0)
                {{
                    _executionContext.EntityFrameworkContext.ClearCache();
                    _executionContext.EntityFrameworkContext.{0}_{1}.AddRange(inserted);
                    _executionContext.EntityFrameworkContext.SaveChanges();
                }}

                _executionContext.EntityFrameworkContext.ClearCache();
            }}
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbException)
            {{
                var sqlException = Rhetos.Utilities.MsSqlUtility.ProcessSqlException(dbException);
                if (sqlException != null)
                    Rhetos.Utilities.ExceptionsUtility.Rethrow(sqlException);
                throw;
            }}

            deleted = null;
            updated = QueryPersisted(updatedNew);
            inserted = QueryPersisted(insertedNew);

            bool allEffectsCompleted = false;
            try
            {{
                " + OnSaveTag1.Evaluate(info) + @"

                " + OnSaveTag2.Evaluate(info) + @"

                allEffectsCompleted = true;
            }}
            finally
            {{
                if (!allEffectsCompleted)
                    _executionContext.PersistenceTransaction.DiscardChanges();
            }}
        }}

",
                info.Module.Name,
                info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode("IWritableRepository<" + info.Module.Name + "." + info.Name + ">", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ExceptionsUtility));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.CsUtility));
            }
        }
    }
}
