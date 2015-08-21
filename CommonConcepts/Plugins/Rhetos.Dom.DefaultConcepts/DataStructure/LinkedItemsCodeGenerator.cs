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
    [ExportMetadata(MefProvider.Implements, typeof(LinkedItemsInfo))]
    public class LinkedItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LinkedItemsInfo)conceptInfo;
            string propertyType = string.Format("IList<Common.Queryable.{0}_{1}>", info.ReferenceProperty.DataStructure.Module.Name, info.ReferenceProperty.DataStructure.Name);

            if (DslUtility.IsQueryable(info.ReferenceProperty.DataStructure) && DslUtility.IsQueryable(info.DataStructure))
                DataStructureQueryableCodeGenerator.AddNavigationPropertyWithBackingField(codeBuilder, info.DataStructure,
                    csPropertyName: info.Name,
                    propertyType: propertyType,
                    additionalSetterCode: null);

            if (info.ReferenceProperty.DataStructure is IOrmDataStructure && info.DataStructure is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().HasOptional(t => t.{2}).WithMany(t => t.{3}).HasForeignKey(t => t.{2}ID);\r\n            ",
                        info.ReferenceProperty.DataStructure.Module.Name, info.ReferenceProperty.DataStructure.Name, info.ReferenceProperty.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.DataStructure is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.{2});\r\n            ",
                        info.DataStructure.Module.Name, info.DataStructure.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
        }
    }
}
