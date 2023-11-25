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
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceInfo))]
    public class UniqueReferenceCodeGenerator : IConceptCodeGenerator
    {
        private readonly ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public UniqueReferenceCodeGenerator(ISqlResources sqlResources, ISqlUtility sqlUtility, ConceptMetadata conceptMetadata)
        {
            Sql = sqlResources;
            SqlUtility = sqlUtility;
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueReferenceInfo)conceptInfo;

            if (DslUtility.IsQueryable(info.Extension) && DslUtility.IsQueryable(info.Base))
            {
                DataStructureQueryableCodeGenerator.AddNavigationProperty(codeBuilder, info.Extension,
                    csPropertyName: "Base",
                    propertyType: "Common.Queryable." + info.Base.Module.Name + "_" + info.Base.Name);
                DataStructureQueryableCodeGenerator.AddNavigationProperty(codeBuilder, info.Base,
                    csPropertyName: info.ExtensionPropertyName(),
                    propertyType: "Common.Queryable." + info.Extension.Module.Name + "_" + info.Extension.Name);
            }

            if (new UniqueReferenceDatabaseDefinition(Sql, SqlUtility).IsSupported(info)
                && info.Extension is IOrmDataStructure
                && info.Base is IWritableOrmDataStructure)
            {
                string systemMessage = $"DataStructure:{info.Extension.FullName},Property:ID,Referenced:{info.Base.FullName}";
                string table = _conceptMetadata.GetOrmSchema(info.Extension) + "." + _conceptMetadata.GetOrmDatabaseObject(info.Extension);
                string constraintName = new UniqueReferenceDatabaseDefinition(Sql, SqlUtility).GetConstraintName(info);
                string onDeleteInterpretSqlError = $"({CsUtility.QuotedString(table)}, {CsUtility.QuotedString(constraintName)}) => {CsUtility.QuotedString(systemMessage)},\r\n                    ";

                codeBuilder.InsertCode(onDeleteInterpretSqlError, WritableOrmDataStructureCodeGenerator.ErrorMetadataTag, info.Base);
            }
        }
    }
}
