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
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DomInitializationCodeGenerator))]
    public class EntityHistoryInfractructureCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(
                "internal class DontTrackHistory<T> : List<T>\r\n    {\r\n    }\r\n    ",
                ModuleCodeGenerator.CommonNamespaceMembersTag);
        }
    }

    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityHistoryInfo))]
    public class EntityHistoryCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<EntityHistoryInfo> ClonePropertiesTag = "CloneProperties";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityHistoryInfo)conceptInfo;
            codeBuilder.InsertCode(FilterImplementationSnippet(info), RepositoryHelper.RepositoryMembers, info.Dependency_ChangesEntity);
            codeBuilder.InsertCode(CreateHistoryOnUpdateSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Entity);
        }

        /// <summary>
        /// Creates a DateTime filter that returns the "Entity"_Changes records that were active at the time (including the current records in the base entity).
        /// AllProperties concept (EntityHistoryAllPropertiesInfo) creates a similar filter on the base Entity class.
        /// </summary>
        private static string FilterImplementationSnippet(EntityHistoryInfo info)
        {
            return string.Format(
        @"public global::{0}.{1}[] Filter(System.DateTime parameter)
        {{
            var sql = ""SELECT * FROM {2}.{3}(@p0)"";
            var query = _executionContext.EntityFrameworkContext.Database.SqlQuery<{0}.{1}>(sql, parameter);
            return query.ToArray();
        }}

        ",
            info.Dependency_ChangesEntity.Module.Name,
            info.Dependency_ChangesEntity.Name,
            SqlUtility.Identifier(info.Entity.Module.Name),
            SqlUtility.Identifier(info.Entity.Name + "_AtTime"));
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

                if (updatedNew.Count() > 0 && !(updatedNew is Common.DontTrackHistory<{0}.{1}>))
			    {{
				    var createHistory = updatedNew.Zip(updated, (newItem, oldItem) => new {{ newItem, oldItem }})
					    .Where(change => (change.oldItem.ActiveSince == null || change.newItem.ActiveSince > change.oldItem.ActiveSince.Value.AddSeconds(errorMarginSeconds)))
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
            }}

",
            info.Entity.Module.Name,
            info.Entity.Name,
            ClonePropertiesTag.Evaluate(info));
        }
    }
}
