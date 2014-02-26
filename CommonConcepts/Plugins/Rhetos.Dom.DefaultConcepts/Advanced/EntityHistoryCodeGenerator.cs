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
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityHistoryInfo))]
    public class EntityHistoryCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<EntityHistoryInfo> ClonePropertiesTag = "CloneProperties";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityHistoryInfo)conceptInfo;
            codeBuilder.InsertCode(FilterInterfaceSnippet(info), RepositoryHelper.RepositoryInterfaces, info.ChangesEntity);
            codeBuilder.InsertCode(FilterImplementationSnippet(info), RepositoryHelper.RepositoryMembers, info.ChangesEntity);
            codeBuilder.InsertCode(AdditionalParameterSnippet(info), DataStructureCodeGenerator.BodyTag, info.Entity);
            codeBuilder.InsertCode(CreateHistoryOnUpdateSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Entity);
            codeBuilder.InsertCode(VerifyChangesEntityTimeSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.ChangesEntity);
        }

        private static string FilterInterfaceSnippet(EntityHistoryInfo info)
        {
            return "IFilterRepository<System.DateTime, " + info.ChangesEntity.Module.Name + "." + info.ChangesEntity.Name + ">";
        }

        /// <summary>
        /// Creates a DateTime filter that returns the "Entity"_Changes records that were active at the time (including the current records in the base entity).
        /// AllProperties concept (EntityHistoryAllPropertiesInfo) creates a similar filter on the base Entity class.
        /// </summary>
        private static string FilterImplementationSnippet(EntityHistoryInfo info)
        {
            return string.Format(
@"        public global::{0}.{1}[] Filter(System.DateTime parameter)
        {{
            var sql = ""SELECT * FROM {2}.{3}(:dateTime)"";
            var result = _executionContext.NHibernateSession.CreateSQLQuery(sql)
                .AddEntity(typeof({0}.{1}))
                .SetTimestamp(""dateTime"", parameter)
                .List<{0}.{1}>();
            return result.ToArray();
        }}

",
            info.ChangesEntity.Module.Name,
            info.ChangesEntity.Name,
            SqlUtility.Identifier(info.Entity.Module.Name),
            SqlUtility.Identifier(info.Entity.Name + "_AtTime"));
        }

        /// <summary>
        /// Additional parameter _createChangesEntryOnEdit. It is used in History update.
        /// If active item is edited through history, it should not create new entry in Changes table.
        /// </summary>
        private static string AdditionalParameterSnippet(EntityHistoryInfo info)
        {
            return
@"
        private bool _overwriteCurrentRecordOnUpdate;
        public virtual void SetOverwriteCurrentRecordOnUpdate(bool value) { this._overwriteCurrentRecordOnUpdate = value; }
        public virtual bool GetOverwriteCurrentRecordOnUpdate() { return this._overwriteCurrentRecordOnUpdate; }
";
        }

        private static string CreateHistoryOnUpdateSnippet(EntityHistoryInfo info)
        {
            return string.Format(
@"			if (insertedNew.Count() > 0 || updatedNew.Count() > 0)
            {{
                var now = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);

                const double errorMarginSeconds = 0.01; // Including database DataTime type imprecision.
                
                foreach (var newItem in insertedNew.Concat(updatedNew))
                    if (newItem.ActiveSince == null)
                        newItem.ActiveSince = now;
                    else if (newItem.ActiveSince > now.AddSeconds(errorMarginSeconds))
                        throw new Rhetos.UserException(string.Format(
                            ""It is not allowed to enter a future time in {0}.{1}.ActiveSince ({{0}}). Set the property value to NULL to automatically use current time ({{1}})."",
                            newItem.ActiveSince, now));

                if (updatedNew.Count() > 0)
			    {{
				    var createHistory = updatedNew.Zip(updated, (newItem, oldItem) => new {{ newItem, oldItem }})
					    .Where(change => (change.oldItem.ActiveSince == null || change.newItem.ActiveSince > change.oldItem.ActiveSince.Value.AddSeconds(errorMarginSeconds))
                            && !change.newItem.GetOverwriteCurrentRecordOnUpdate())
					    .Select(change => change.oldItem)
					    .ToArray();
					
				    _domRepository.{0}.{1}_Changes.Insert(
					    createHistory.Select(olditem =>
						    new {0}.{1}_Changes
						    {{
                                ID = Guid.NewGuid(),
							    EntityID = olditem.ID{2}
						    }}).ToArray());
			    }}

                // Workaround to restore NH proxies if NHSession.Clear() is called inside filter.
                for (int i=0; i<updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(updated[i].ID);
                for (int i=0; i<deleted.Length; i++) deleted[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(deleted[i].ID);
            }}

",
            info.Entity.Module.Name,
            info.Entity.Name,
            ClonePropertiesTag.Evaluate(info));
        }

        private static string VerifyChangesEntityTimeSnippet(EntityHistoryInfo info)
        {
            return string.Format(
@"			if (insertedNew.Count() > 0 || updatedNew.Count() > 0)
            {{
                var now = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);
                
                foreach (var newItem in insertedNew.Concat(updatedNew))
                    if (newItem.ActiveSince > now)
                        throw new Rhetos.UserException(string.Format(
                            ""It is not allowed to enter a future time in {0}.{1}.ActiveSince ({{0}}). Current server time is {{1}}."",
                            newItem.ActiveSince, now));
            }}

",
            info.ChangesEntity.Module.Name,
            info.ChangesEntity.Name);
        }
    }
}
