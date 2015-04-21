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
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ReferencePropertyInfo info = (ReferencePropertyInfo)conceptInfo;

            var referenceGuid = new PropertyInfo { DataStructure = info.DataStructure, Name = info.Name + "ID" };
            PropertyHelper.GenerateCodeForType(referenceGuid, codeBuilder, "Guid?");

            if (DslUtility.IsQueryable(info.DataStructure) && DslUtility.IsQueryable(info.Referenced))
                DataStructureQueryableCodeGenerator.AddNavigationProperty(codeBuilder, info.DataStructure, info.Name, "Common.Queryable." + info.Referenced.Module.Name + "_" + info.Referenced.Name, info.Name + "ID");

            if (info.DataStructure is IOrmDataStructure && info.Referenced is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().HasOptional(t => t.{2}).WithMany().HasForeignKey(t => t.{2}ID);\r\n            ",
                        info.DataStructure.Module.Name, info.DataStructure.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            else if (info.DataStructure is IOrmDataStructure)
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Entity<Common.Queryable.{0}_{1}>().Ignore(t => t.{2});\r\n            ",
                        info.DataStructure.Module.Name, info.DataStructure.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
        }
    }
}
