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
        public class UniqueMultipleTag : Tag<UniqueMultiplePropertiesInfo>
        {
            public UniqueMultipleTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format,
                    info.DataStructure.Module.Name, // {0}
                    info.DataStructure.Name, // {1}
                    CsUtility.TextToIdentifier(info.PropertyNames)), // {2}
                    nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        public static readonly UniqueMultipleTag ColumnListTag = new UniqueMultipleTag(TagType.Appendable,
            "/*UniqueMultiple.ColumnList {0}.{1}.{2}*/", "/*next UniqueMultiple.ColumnList {0}.{1}.{2}*/", "{0}", ", {0}");

        public static readonly UniqueMultipleTag ColumnJoinTag = new UniqueMultipleTag(TagType.Appendable,
            "/*UniqueMultiple.ColumnJoin {0}.{1}.{2}*/", "/*next UniqueMultiple.ColumnJoin {0}.{1}.{2}*/", "{0}", " AND {0}");

        public static readonly UniqueMultipleTag PropertyListTag = new UniqueMultipleTag(TagType.Appendable,
            "/*UniqueMultiple.PropertyList {0}.{1}.{2}*/", "/*next UniqueMultiple.PropertyList {0}.{1}.{2}*/", "{0}", " + \", \" + {0}");

        public static readonly UniqueMultipleTag PropertyValuesTag = new UniqueMultipleTag(TagType.Appendable,
            "/*UniqueMultiple.PropertyValues {0}.{1}.{2}*/", "/*next UniqueMultiple.PropertyValues {0}.{1}.{2}*/", "{0}", " + \", \" + {0}");


        public static bool IsSupported(UniqueMultiplePropertiesInfo info)
        {
            return !UniqueMultiplePropertiesInfo.SqlImplementation(info)
                && info is IWritableOrmDataStructure;
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
            return string.Format(@"
            {{
                const string sql = @""SELECT source.*
                    FROM {0}.{1} source
                    INNER JOIN (SELECT {2} FROM {0}.{1} GROUP BY {2} HAVING COUNT(*) > 1) doubles
                        ON {3}"";

                var nhSqlQuery = _executionContext.NHibernateSession.CreateSQLQuery(sql).AddEntity(typeof({0}.{1}));
                var invalidItems = nhSqlQuery.List<{0}.{1}>();

                IEnumerable<Guid> changesItems = inserted.Select(item => item.ID).Union(updated.Select(item => item.ID));
                var changesItemsSet = new HashSet<Guid>(changesItems);
                invalidItems = invalidItems.Where(invalidItem => changesItemsSet.Contains(invalidItem.ID)).ToList();
                
                if (invalidItems.Count() > 0)
                {{
                    string msg = ""It is not allowed to enter a duplicate record. A record with the same value ("" + {4} + "") already exists in the system: "";
                    var invalidItem = invalidItems.First();
                    msg += ""("" + {5} + "")."";
                    throw new Rhetos.UserException(msg);
                }}
            }}
",
            info.DataStructure.Module.Name,
            info.DataStructure.Name,
            ColumnListTag.Evaluate(info),
            ColumnJoinTag.Evaluate(info),
            PropertyListTag.Evaluate(info),
            PropertyValuesTag.Evaluate(info));
        }
    }
}
