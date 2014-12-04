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
        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds" but without navigation properties, because they are not binded to ORM.</summary>
        public static readonly CsTag<DataStructureInfo> ArgumentValidationTag = "WritableOrm ArgumentValidation";

        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds" but without navigation properties, because they are not binded to ORM.</summary>
        public static readonly CsTag<DataStructureInfo> InitializationTag = "WritableOrm Initialization";

        /// <summary>Arrays "updated" and "deleted" contain OLD data.
        /// Enumerables "insertedNew", "updatedNew" and "deletedIds" are not binded to ORM (do not use navigation properties).
        /// Sample usage: 1. Verify that locked items are not goind to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly CsTag<DataStructureInfo> OldDataLoadedTag = "WritableOrm OldDataLoaded";

        [Obsolete]
        public static readonly CsTag<DataStructureInfo> ProcessedOldDataTag = "WritableOrm ProcessedOldData";

        /// <summary> Recomended usage: Recompute items that depend on changes items.
        /// Arrays "inserted" and "updated" contain NEW data.
        /// Data is saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations CAN be used.</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag1 = "WritableOrm OnSaveTag1";

        /// <summary>Recomended usage: Verify that invalid items are not going to be inserted or updated.
        /// Arrays "inserted" and "updated" contain NEW data.
        /// Data is saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations CAN be used.</summary>
        public static readonly CsTag<DataStructureInfo> OnSaveTag2 = "WritableOrm OnSaveTag2";

        // TODO: Remove "duplicateObjects" check after implementing NHibernate stateless session with "manual" saving.
        protected static string MemberFunctionsSnippet(DataStructureInfo info)
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

            _executionContext.NHibernateSession.Clear(); // Updating a modified persistent object could break old-data validations such as checking for locked items.

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

            {6}

{1}

            // Using old data:
            {0}[] updated = updatedNew.Select(item => _executionContext.NHibernateSession.Load<{0}>(item.ID)).ToArray();
            {0}[] deleted = deletedIds.Select(item => _executionContext.NHibernateSession.Load<{0}>(item.ID)).ToArray();

{2}

{3}

            {0}[] inserted;

            try
            {{
                foreach (var item in deleted)
                    _executionContext.NHibernateSession.Delete(item);
                deleted = null;
                _executionContext.NHibernateSession.Flush();

                updated = updatedNew.Select(item => ({0})_executionContext.NHibernateSession.Merge(item)).ToArray();
                _executionContext.NHibernateSession.Flush();

                inserted = insertedNew.Select(item => ({0})_executionContext.NHibernateSession.Merge(item)).ToArray();
                _executionContext.NHibernateSession.Flush();

                // Clearing lazy object cache to ensure loading fresh data from database, in case of data modifications by triggers.
                _executionContext.NHibernateSession.Clear();
                for (int i=0; i<inserted.Length; i++) inserted[i] = _executionContext.NHibernateSession.Load<{0}>(inserted[i].ID);
                for (int i=0; i<updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{0}>(updated[i].ID);
            }}
            catch (NHibernate.Exceptions.GenericADOException nhException)
            {{
                var sqlException = Rhetos.Utilities.MsSqlUtility.ProcessSqlException(nhException.InnerException);
                if (sqlException != null)
                    throw sqlException;
                throw;
            }}

            bool allEffectsCompleted = false;
            try
            {{
{4}

{5}

                allEffectsCompleted = true;
            }}
            finally
            {{
                if (!allEffectsCompleted)
                    _executionContext.PersistenceTransaction.DiscardChanges();
            }}
        }}

",
                info.GetKeyProperties(),
                InitializationTag.Evaluate(info),
                OldDataLoadedTag.Evaluate(info),
                ProcessedOldDataTag.Evaluate(info),
                OnSaveTag1.Evaluate(info),
                OnSaveTag2.Evaluate(info),
                ArgumentValidationTag.Evaluate(info));
        }

        protected static string RegisterRepository(DataStructureInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IWritableRepository>(""{0}.{1}"");
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode("IWritableRepository", RepositoryHelper.RepositoryInterfaces, info);
                codeBuilder.InsertCode(MemberFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
                codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ExceptionsUtility));
            }
        }
    }
}
