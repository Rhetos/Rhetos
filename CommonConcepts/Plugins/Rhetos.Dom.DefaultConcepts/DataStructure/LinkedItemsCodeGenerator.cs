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
        public class LinkedItemsTag : Tag<LinkedItemsInfo>
        {
            public LinkedItemsTag(TagType tagType, string tagFormat)
                : base(tagType, tagFormat, (info, format) =>
                    string.Format(CultureInfo.InvariantCulture,
                        format,
                            info.DataStructure.Module.Name,
                            info.DataStructure.Name,
                            info.Name,
                            info.ReferenceProperty.Name))
            { }
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LinkedItemsInfo)conceptInfo;
            PropertyHelper.GenerateCodeForType(
                info, 
                codeBuilder, 
                string.Format(CultureInfo.InvariantCulture,
                    "IList<{0}.{1}>",  
                        info.ReferenceProperty.DataStructure.Module.Name,
                        info.ReferenceProperty.DataStructure.Name),
                false);
            codeBuilder.InsertCode(
                string.Format(CultureInfo.InvariantCulture,
                    " = new List<{0}.{1}>()",
                        info.ReferenceProperty.DataStructure.Module.Name,
                        info.ReferenceProperty.DataStructure.Name),
                PropertyHelper.DefaultValueTag,
                info);
            codeBuilder.InsertCode(
                string.Format(CultureInfo.InvariantCulture, 
@"            foreach(var item in insertedNew)
                item.{0} = _executionContext.NHibernateSession.Query<{1}.{2}>().Where(it => it.{3} == item).ToList();
            foreach(var item in updatedNew)
                item.{0} = _executionContext.NHibernateSession.Query<{1}.{2}>().Where(it => it.{3} == item).ToList();

", info.Name, info.ReferenceProperty.DataStructure.Module.Name, info.ReferenceProperty.DataStructure.Name, info.ReferenceProperty.Name),
                WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.DataStructure));
        }
    }
}
