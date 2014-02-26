﻿/*
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
    [ExportMetadata(MefProvider.Implements, typeof(EntityHistoryAllPropertiesInfo))]
    public class EntityHistoryAllPropertiesCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityHistoryAllPropertiesInfo)conceptInfo;
            codeBuilder.InsertCode(FilterInterfaceSnippet(info), RepositoryHelper.RepositoryInterfaces, info.EntityHistory.Entity);
            codeBuilder.InsertCode(FilterImplementationSnippet(info), RepositoryHelper.RepositoryMembers, info.EntityHistory.Entity);
        }

        private static string FilterInterfaceSnippet(EntityHistoryAllPropertiesInfo info)
        {
            return "IFilterRepository<System.DateTime, " + info.EntityHistory.Entity.Module.Name + "." + info.EntityHistory.Entity.Name + ">";
        }

        /// <summary>
        /// Creates a DateTime filter that returns the entity records that were active at the time.
        /// History concept creates a similar filter on the "Entity"_Changes repository, but this filter on the base entity class
        /// can only be created of all properties are selected for history tracking, therefore it is implemented in EntityHistoryAllPropertiesInfo.
        /// </summary>
        private static string FilterImplementationSnippet(EntityHistoryAllPropertiesInfo info)
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
            info.EntityHistory.Entity.Module.Name,
            info.EntityHistory.Entity.Name,
            SqlUtility.Identifier(info.EntityHistory.Entity.Module.Name),
            SqlUtility.Identifier(info.EntityHistory.Entity.Name + "_AtTime"));
        }
    }
}
