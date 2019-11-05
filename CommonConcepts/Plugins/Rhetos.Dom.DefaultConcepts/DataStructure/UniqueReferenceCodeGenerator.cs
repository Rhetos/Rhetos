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

using Rhetos.Dsl.DefaultConcepts;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator.DefaultConcepts;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceInfo))]
    public class UniqueReferenceCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueReferenceInfo)conceptInfo;

            if (DslUtility.IsQueryable(info.Extension) && DslUtility.IsQueryable(info.Base))
            {
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.Extension,
                    csPropertyName: "Base",
                    propertyType: "Common.Queryable." + info.Base.Module.Name + "_" + info.Base.Name,
                    additionalSetterCode: "ID = value != null ? value.ID : Guid.Empty;");
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.Base,
                    csPropertyName: info.ExtensionPropertyName(),
                    propertyType: "Common.Queryable." + info.Extension.Module.Name + "_" + info.Extension.Name,
                    additionalSetterCode: null);
            }

            if (info.Extension is IOrmDataStructure && info.Base is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().HasRequired(t => t.Base).WithOptional(t => t.{2});\r\n            ",
                        info.Extension.Module.Name, info.Extension.Name, info.ExtensionPropertyName()),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.Extension is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.Base);\r\n            ",
                        info.Extension.Module.Name, info.Extension.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.Base is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.{2});\r\n            ",
                        info.Base.Module.Name, info.Base.Name, info.ExtensionPropertyName()),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);

            if (UniqueReferenceDatabaseDefinition.IsSupported(info)
                && info.Extension is IOrmDataStructure
                && info.Base is IWritableOrmDataStructure)
            {
                var ormDataStructure = (IOrmDataStructure)info.Extension;
                string systemMessage = $"DataStructure:{info.Extension.FullName},Property:ID,Referenced:{info.Base.FullName}";
                string onDeleteInterpretSqlError = @"if (interpretedException is Rhetos.UserException && Rhetos.Utilities.MsSqlUtility.IsReferenceErrorOnDelete(interpretedException, "
                    + CsUtility.QuotedString(ormDataStructure.GetOrmSchema() + "." + ormDataStructure.GetOrmDatabaseObject()) + @", "
                    + CsUtility.QuotedString("ID") + @", "
                    + CsUtility.QuotedString(UniqueReferenceDatabaseDefinition.GetConstraintName(info)) + @"))
                    ((Rhetos.UserException)interpretedException).SystemMessage = " + CsUtility.QuotedString(systemMessage) + @";
                ";
                codeBuilder.InsertCode(onDeleteInterpretSqlError, WritableOrmDataStructureCodeGenerator.OnDatabaseErrorTag, info.Base);
            }
        }
    }
}
