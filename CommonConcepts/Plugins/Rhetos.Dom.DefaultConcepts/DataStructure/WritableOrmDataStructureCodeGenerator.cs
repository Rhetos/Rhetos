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
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(OrmDataStructureCodeGenerator))]
    public class WritableOrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds" but without navigation properties because they are not binded to ORM.</summary>
        public static readonly DataStructureCodeGenerator.DataStructureTag InitializationTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*WritableOrm.Initialization {0}.{1}*/");

        /// <summary>Arrays "updated" and "deleted" contain OLD data.
        /// Enumerables "insertedNew", "updatedNew" and "deletedIds" are not binded to ORM (do not use navigation properties).
        /// Sample usage: 1. Verify that locked items are not goind to be updated or deleted, 2. Cascade delete.</summary>
        public static readonly DataStructureCodeGenerator.DataStructureTag OldDataLoadedTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*WritableOrm.OldDataLoaded {0}.{1}*/");

        /// <summary>Arrays "inserted" and "updated" contain NEW data.
        /// Data is not yet saved to database so SQL validations and computations can NOT be used).</summary>
        public static readonly DataStructureCodeGenerator.DataStructureTag NewDataLoadedTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*WritableOrm.NewDataLoaded {0}.{1}*/");

        /// <summary> Recomended usage: Recompute items that depend on changes items.
        /// Arrays "inserted" and "updated" contain NEW data.
        /// Data is saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations CAN be used.</summary>
        public static readonly DataStructureCodeGenerator.DataStructureTag OnSaveTag1 =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*WritableOrm.OnSaveTag1 {0}.{1}*/");

        /// <summary>Recomended usage: Verify that invalid items are not going to be inserted or updated.
        /// Arrays "inserted" and "updated" contain NEW data.
        /// Data is saved to the database (but the SQL transaction has not yet been commited) so SQL validations and computations CAN be used.</summary>
        public static readonly DataStructureCodeGenerator.DataStructureTag OnSaveTag2 =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*WritableOrm.OnSaveTag2 {0}.{1}*/");


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
            if (insertedNew == null) insertedNew = new {0}[] {{ }};
            if (updatedNew == null) updatedNew = new {0}[] {{ }};
            if (deletedIds == null) deletedIds = new {0}[] {{ }};

            if (insertedNew.Count() == 0 && updatedNew.Count() == 0 && deletedIds.Count() == 0)
                return;

            foreach(var item in insertedNew)
                if(item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

            _executionContext.NHibernateSession.Clear(); // Updating a modified persistent object could break old-data validations such as checking for locked items.

            if (insertedNew.Count() > 0)
            {{
                var existingObject = Filter(insertedNew.Select(item => item.ID).ToArray()).FirstOrDefault();
                if (existingObject != null)
                    throw new Rhetos.FrameworkException(""Inserting already existing object. ID="" + existingObject.ID);
            }}

{1}

            // Using old data:
            {0}[] updated = updatedNew.Select(item => _executionContext.NHibernateSession.Load<{0}>(item.ID)).ToArray();
            {0}[] deleted = deletedIds.Select(item => _executionContext.NHibernateSession.Load<{0}>(item.ID)).ToArray();

{2}

            // Using new data:
            {0}[] inserted = insertedNew.Select(item => ({0})_executionContext.NHibernateSession.Merge(item)).ToArray();
            updated = updatedNew.Select(item => ({0})_executionContext.NHibernateSession.Merge(item)).ToArray();
            foreach (var item in deleted)
                _executionContext.NHibernateSession.Delete(item);
            deleted = null;

{3}

            try
            {{
                _executionContext.NHibernateSession.Flush();
            }}
            catch (NHibernate.Exceptions.GenericADOException nhException)
            {{
                var newException = Rhetos.Utilities.MsSqlUtility.ProcessSqlException(nhException.InnerException);
                if (newException != null)
                    throw newException;
                throw;
            }}

{4}

{5}
        }}

",
                info.GetKeyProperties(),
                InitializationTag.Evaluate(info),
                OldDataLoadedTag.Evaluate(info),
                NewDataLoadedTag.Evaluate(info),
                OnSaveTag1.Evaluate(info),
                OnSaveTag2.Evaluate(info));
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
            }
        }
    }
}
