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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureExtendsInfo))]
    public class DataStructureExtendsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureExtendsInfo info = (DataStructureExtendsInfo)conceptInfo;
            var extensionPropertyName = ExtensionPropertyName(info);

            if (DslUtility.IsQueryable(info.Extension) && DslUtility.IsQueryable(info.Base))
            {
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.Extension,
                    csPropertyName: "Base",
                    propertyType: "Common.Queryable." + info.Base.Module.Name + "_" + info.Base.Name,
                    additionalSetterCode: "ID = value != null ? value.ID : Guid.Empty;");
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.Base,
                    csPropertyName: extensionPropertyName,
                    propertyType: "Common.Queryable." + info.Extension.Module.Name + "_" + info.Extension.Name,
                    additionalSetterCode: null);
            }

            if (info.Extension is IOrmDataStructure && info.Base is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().HasRequired(t => t.Base).WithOptional(t => t.{2});\r\n            ",
                        info.Extension.Module.Name, info.Extension.Name, extensionPropertyName),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.Extension is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.Base);\r\n            ",
                        info.Extension.Module.Name, info.Extension.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.Base is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.{2});\r\n            ",
                        info.Base.Module.Name, info.Base.Name, extensionPropertyName),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
        }

        public static string ExtensionPropertyName(DataStructureExtendsInfo info)
        {
            if (info.Base.Module == info.Extension.Module)
                return "Extension_" + info.Extension.Name;
            return "Extension_" + info.Extension.Module.Name + "_" + info.Extension.Name;
        }
    }
}
