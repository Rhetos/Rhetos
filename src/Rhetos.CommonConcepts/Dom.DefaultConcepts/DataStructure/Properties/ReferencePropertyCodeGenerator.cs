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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyCodeGenerator : IConceptCodeGenerator
    {
        private readonly ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public ReferencePropertyCodeGenerator(ISqlResources sqlResources, ISqlUtility sqlUtility, ConceptMetadata conceptMetadata)
        {
            Sql = sqlResources;
            SqlUtility = sqlUtility;
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ReferencePropertyInfo info = (ReferencePropertyInfo)conceptInfo;

            var referenceGuid = new PropertyInfo { DataStructure = info.DataStructure, Name = info.Name + "ID" };
            PropertyHelper.GenerateCodeForType(referenceGuid, codeBuilder, "Guid?");
            PropertyHelper.GenerateStorageMapping(referenceGuid, codeBuilder, "System.Data.SqlDbType.UniqueIdentifier");

            if (DslUtility.IsQueryable(info.DataStructure) && DslUtility.IsQueryable(info.Referenced))
                DataStructureQueryableCodeGenerator.AddNavigationProperty(codeBuilder, info.DataStructure,
                    csPropertyName: info.Name,
                    propertyType: "Common.Queryable." + info.Referenced.Module.Name + "_" + info.Referenced.Name);

            if (new ReferencePropertyDbConstraintMacro(SqlUtility).IsSupported(info)
                && info.DataStructure is IOrmDataStructure
                && info.Referenced is IOrmDataStructure)
            {
                string systemMessage = $"DataStructure:{info.DataStructure.FullName},Property:{info.Name}ID,Referenced:{info.Referenced.FullName}";

                if (info.DataStructure is IWritableOrmDataStructure)
                {
                    string onEnterInterpretSqlError = @"if (interpretedException is Rhetos.UserException && Rhetos.Utilities.MsSqlUtility.IsReferenceErrorOnInsertUpdate(interpretedException, "
                        + CsUtility.QuotedString(_conceptMetadata.GetOrmSchema(info.Referenced) + "." + _conceptMetadata.GetOrmDatabaseObject(info.Referenced)) + @", "
                        + CsUtility.QuotedString("ID") + @", "
                        + CsUtility.QuotedString(new ReferencePropertyConstraintDatabaseDefinition(Sql, SqlUtility).GetConstraintName(info)) + @"))
                        ((Rhetos.UserException)interpretedException).SystemMessage = " + CsUtility.QuotedString(systemMessage) + @";
                    ";
                    codeBuilder.InsertCode(onEnterInterpretSqlError, WritableOrmDataStructureCodeGenerator.OnDatabaseErrorTag, info.DataStructure);

                    if (info.Referenced == info.DataStructure)
                        codeBuilder.InsertCode($@"if (entity.{info.Name}ID != null && entity.{info.Name}ID != entity.ID) yield return entity.{info.Name}ID.Value;
            ", WritableOrmDataStructureCodeGenerator.PersistenceStorageMapperDependencyResolutionTag, info.DataStructure);
                }

                if (info.Referenced is IWritableOrmDataStructure)
                {
                    string onDeleteInterpretSqlError = @"if (interpretedException is Rhetos.UserException && Rhetos.Utilities.MsSqlUtility.IsReferenceErrorOnDelete(interpretedException, "
                        + CsUtility.QuotedString(_conceptMetadata.GetOrmSchema(info.DataStructure) + "." + _conceptMetadata.GetOrmDatabaseObject(info.DataStructure)) + @", "
                        + CsUtility.QuotedString(info.GetSimplePropertyName()) + @", "
                        + CsUtility.QuotedString(new ReferencePropertyConstraintDatabaseDefinition(Sql, SqlUtility).GetConstraintName(info)) + @"))
                        ((Rhetos.UserException)interpretedException).SystemMessage = " + CsUtility.QuotedString(systemMessage) + @";
                    ";
                    codeBuilder.InsertCode(onDeleteInterpretSqlError, WritableOrmDataStructureCodeGenerator.OnDatabaseErrorTag, info.Referenced);
                }
            }
        }
    }
}
