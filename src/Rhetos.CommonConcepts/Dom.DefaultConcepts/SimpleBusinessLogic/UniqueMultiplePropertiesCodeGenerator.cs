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
using Rhetos.DatabaseGenerator;
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts.SimpleBusinessLogic
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueMultiplePropertiesInfo))]
    public class UniqueMultiplePropertiesCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<UniqueMultiplePropertiesInfo> ColumnListTag = new CsTag<UniqueMultiplePropertiesInfo>("ColumnList", TagType.Appendable, "{0}", ", {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> ColumnJoinTag = new CsTag<UniqueMultiplePropertiesInfo>("ColumnJoin", TagType.Appendable, "{0}", " AND {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> PropertyListTag = new CsTag<UniqueMultiplePropertiesInfo>("PropertyList", TagType.Appendable, "{0}", " + \", \" + {0}");
        public static readonly CsTag<UniqueMultiplePropertiesInfo> PropertyValuesTag = new CsTag<UniqueMultiplePropertiesInfo>("PropertyValues", TagType.Appendable, "{0}", " + \", \" + {0}");
        private readonly ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public UniqueMultiplePropertiesCodeGenerator(ISqlResources sqlResources, ISqlUtility sqlUtility, ConceptMetadata conceptMetadata)
        {
            Sql = sqlResources;
            SqlUtility = sqlUtility;
            _conceptMetadata = conceptMetadata;
        }

        public static bool ImplementInObjectModel(UniqueMultiplePropertiesInfo info)
        {
            return !info.Dependency_SqlIndex.SqlImplementation() && info.DataStructure is IWritableOrmDataStructure;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueMultiplePropertiesInfo)conceptInfo;

            if (ImplementInObjectModel(info))
            {
                codeBuilder.InsertCode(CheckSavedItemsSnippet(info), WritableOrmDataStructureCodeGenerator.OnSaveTag2, info.DataStructure);
            }

            if (info.Dependency_SqlIndex.SqlImplementation() && info.DataStructure is IWritableOrmDataStructure)
            {
                string systemMessage = $"DataStructure:{info.DataStructure.FullName},Property:{info.PropertyNames}";
                string table = _conceptMetadata.GetOrmSchema(info.DataStructure) + "." + _conceptMetadata.GetOrmDatabaseObject(info.DataStructure);
                string constraintName = new SqlIndexMultipleDatabaseDefinition(Sql, SqlUtility).ConstraintName(info.Dependency_SqlIndex);
                string interpretSqlError = $"({CsUtility.QuotedString(table)},  {CsUtility.QuotedString(constraintName)}) => {CsUtility.QuotedString(systemMessage)},\r\n                    ";

                codeBuilder.InsertCode(interpretSqlError, WritableOrmDataStructureCodeGenerator.ErrorMetadataTag, info.DataStructure);
            }
        }

        private string CheckSavedItemsSnippet(UniqueMultiplePropertiesInfo info)
        {
            return string.Format(
            @"{{
                    const string sql = @""SELECT source.*
                        FROM {6}.{7} source
                        INNER JOIN (SELECT {2} FROM {6}.{7} GROUP BY {2} HAVING COUNT(*) > 1) doubles
                            ON {3}"";

                    IEnumerable<Guid> changesItems = inserted.Select(item => item.ID).Union(updated.Select(item => item.ID));
                    var changesItemsSet = new HashSet<Guid>(changesItems);

                    var invalidItems = _executionContext.EntityFrameworkContext.LoadFromSql<{0}.{1}>(sql)
                        .Where(invalidItem => changesItemsSet.Contains(invalidItem.ID)).ToList();
                
                    if (invalidItems.Count() > 0)
                    {{
                        var invalidItem = invalidItems.First();
                        string duplicateValue = {4}
                            + "" '"" + {5} + ""'"";
                        throw new Rhetos.UserException(
                            ""It is not allowed to enter a duplicate record in {{0}}. A record with the same value already exists: {{1}}."",
                            new[] {{ _localizer[""{0}.{1}""], duplicateValue }}, null, null);
                    }}
                }}
                ",
            info.DataStructure.Module.Name,
            info.DataStructure.Name,
            ColumnListTag.Evaluate(info),
            ColumnJoinTag.Evaluate(info),
            PropertyListTag.Evaluate(info),
            PropertyValuesTag.Evaluate(info),
            _conceptMetadata.GetOrmSchema(info.DataStructure),
            _conceptMetadata.GetOrmDatabaseObject(info.DataStructure));
        }
    }
}
