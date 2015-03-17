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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.Globalization;

namespace Rhetos.Dom.DefaultConcepts.SimpleBusinessLogic
{
    // TODO: This feature does not have unit tests and should not be used.
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueMultiplePropertiesInfo))]
    public class UniqueMultiplePropertiesCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<UniqueMultiplePropertiesInfo> ColumnListTag = new CsTag<UniqueMultiplePropertiesInfo>("ColumnList", TagType.Appendable, "{0}", ", {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> ColumnJoinTag = new CsTag<UniqueMultiplePropertiesInfo>("ColumnJoin", TagType.Appendable, "{0}", " AND {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> PropertyListTag = new CsTag<UniqueMultiplePropertiesInfo>("PropertyList", TagType.Appendable, "{0}", " + \", \" + {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> PropertyValuesTag = new CsTag<UniqueMultiplePropertiesInfo>("PropertyValues", TagType.Appendable, "{0}", " + \", \" + {0}");


        public static bool IsSupported(UniqueMultiplePropertiesInfo info)
        {
            return !info.SqlImplementation() && info.DataStructure is IWritableOrmDataStructure;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueMultiplePropertiesInfo)conceptInfo;

            if (IsSupported(info))
            {
                codeBuilder.InsertCode(CheckSavedItemsSnippet(info), WritableOrmDataStructureCodeGenerator.OnSaveTag2, info.DataStructure);
                codeBuilder.AddReferencesFromDependency(typeof(UserException));
            }
        }

        private string CheckSavedItemsSnippet(UniqueMultiplePropertiesInfo info)
        {
            return string.Format(@"            {{
                const string sql = @""SELECT source.*
                    FROM {6}.{7} source
                    INNER JOIN (SELECT {2} FROM {6}.{7} GROUP BY {2} HAVING COUNT(*) > 1) doubles
                        ON {3}"";

                var nhSqlQuery = _executionContext.NHibernateSession.CreateSQLQuery(sql).AddEntity(typeof({0}.{1}));
                var invalidItems = nhSqlQuery.List<{0}.{1}>();

                IEnumerable<Guid> changesItems = inserted.Select(item => item.ID).Union(updated.Select(item => item.ID));
                var changesItemsSet = new HashSet<Guid>(changesItems);
                invalidItems = invalidItems.Where(invalidItem => changesItemsSet.Contains(invalidItem.ID)).ToList();
                
                if (invalidItems.Count() > 0)
                {{
                    string msg = ""It is not allowed to enter a duplicate record in {0}.{1}. A record with the same value already exists in the system: "";
                    var invalidItem = invalidItems.First();
                    msg += {4} + "" '"" + {5} + ""'."";
                    throw new Rhetos.UserException(msg);
                }}
            }}
",
            info.DataStructure.Module.Name,
            info.DataStructure.Name,
            ColumnListTag.Evaluate(info),
            ColumnJoinTag.Evaluate(info),
            PropertyListTag.Evaluate(info),
            PropertyValuesTag.Evaluate(info),
            ((IWritableOrmDataStructure)info.DataStructure).GetOrmSchema(),
            ((IWritableOrmDataStructure)info.DataStructure).GetOrmDatabaseObject());
        }
    }
}
