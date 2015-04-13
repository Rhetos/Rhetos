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
        private static string OrmMappingOnSaveSnippet(LinkedItemsInfo info)
        {
            return string.Format(
@"            foreach(var item in insertedNew)
                item.{0} = _executionContext.NHibernateSession.Query<{1}.{2}>().Where(it => it.{3} == item).ToList();
            foreach(var item in updatedNew)
                item.{0} = _executionContext.NHibernateSession.Query<{1}.{2}>().Where(it => it.{3} == item).ToList();

",
                info.Name,
                info.ReferenceProperty.DataStructure.Module.Name,
                info.ReferenceProperty.DataStructure.Name,
                info.ReferenceProperty.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LinkedItemsInfo)conceptInfo;

            string propertyType = string.Format("IList<{0}.{1}>", info.ReferenceProperty.DataStructure.Module.Name, info.ReferenceProperty.DataStructure.Name);
            DataStructureQueryableCodeGenerator.AddProperty(codeBuilder, info.DataStructure, info.Name, propertyType);

            codeBuilder.InsertCode(OrmMappingOnSaveSnippet(info), WritableOrmDataStructureCodeGenerator.InitializationTag, info.DataStructure);
        }
    }
}
