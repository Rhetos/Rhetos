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
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ReferencePropertyInfo info = (ReferencePropertyInfo)conceptInfo;

            var referenceGuid = new PropertyInfo { DataStructure = info.DataStructure, Name = info.Name + "ID" };
            PropertyHelper.GenerateCodeForType(referenceGuid, codeBuilder, "Guid?");

            if (DslUtility.IsQueryable(info.DataStructure) && DslUtility.IsQueryable(info.Referenced))
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.DataStructure,
                    csPropertyName: info.Name,
                    propertyType: "Common.Queryable." + info.Referenced.Module.Name + "_" + info.Referenced.Name,
                    additionalSetterCode: info.Name + "ID = value != null ? (Guid?)value.ID : null;");

            if (info.DataStructure is IOrmDataStructure && info.Referenced is IOrmDataStructure)
                codeBuilder.InsertCode(
                    info.OrmMappingSnippet(),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.DataStructure is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.{2});\r\n            ",
                        info.DataStructure.Module.Name, info.DataStructure.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);

            if (ReferencePropertyConstraintDatabaseDefinition.IsSupported(info)
                && info.DataStructure is IOrmDataStructure
                && info.Referenced is IOrmDataStructure)
            {
                var ormDataStructure = (IOrmDataStructure)info.DataStructure;
                var referencedOrmDataStructure = (IOrmDataStructure)info.Referenced;
                string systemMessage = "DataStructure:" + info.DataStructure + ",Property:" + info.Name + "ID,Referenced:" + info.Referenced;

                if (info.DataStructure is IWritableOrmDataStructure)
                {
                    string onEnterInterpretSqlError = @"if (interpretedException is Rhetos.UserException && Rhetos.Utilities.MsSqlUtility.IsReferenceErrorOnInsertUpdate(interpretedException, "
                        + CsUtility.QuotedString(referencedOrmDataStructure.GetOrmSchema() + "." + referencedOrmDataStructure.GetOrmDatabaseObject()) + @", "
                        + CsUtility.QuotedString("ID") + @", "
                        + CsUtility.QuotedString(ReferencePropertyConstraintDatabaseDefinition.GetConstraintName(info)) + @"))
                    ((Rhetos.UserException)interpretedException).SystemMessage = " + CsUtility.QuotedString(systemMessage) + @";
                ";
                    codeBuilder.InsertCode(onEnterInterpretSqlError, WritableOrmDataStructureCodeGenerator.OnDatabaseErrorTag, info.DataStructure);
                }

                if (info.Referenced is IWritableOrmDataStructure)
                {
                    string onDeleteInterpretSqlError = @"if (interpretedException is Rhetos.UserException && Rhetos.Utilities.MsSqlUtility.IsReferenceErrorOnDelete(interpretedException, "
                        + CsUtility.QuotedString(ormDataStructure.GetOrmSchema() + "." + ormDataStructure.GetOrmDatabaseObject()) + @", "
                        + CsUtility.QuotedString(info.GetSimplePropertyName()) + @", "
                        + CsUtility.QuotedString(ReferencePropertyConstraintDatabaseDefinition.GetConstraintName(info)) + @"))
                    ((Rhetos.UserException)interpretedException).SystemMessage = " + CsUtility.QuotedString(systemMessage) + @";
                ";
                    codeBuilder.InsertCode(onDeleteInterpretSqlError, WritableOrmDataStructureCodeGenerator.OnDatabaseErrorTag, info.Referenced);
                }
            }
        }
    }
}
