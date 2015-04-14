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
        private static string ReferenceIDSnippet(ReferencePropertyInfo info, PropertyInfo referenceGuid)
        {
            return string.Format(
@"{3}
        public virtual Guid? {0}ID
        {{
            get
            {{
                if ({0} != null)
                    return {0}.ID;
                return null;
            }}
            set
            {{
                if(value == null)
                    {0} = null;
                else
                    {0} = new {1}.{2} {{ ID = value.Value }};
            }}
        }}

        ",
            info.Name,
            info.Referenced.Module.Name,
            info.Referenced.Name,
            PropertyHelper.AttributeTag.Evaluate(referenceGuid));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ReferencePropertyInfo info = (ReferencePropertyInfo)conceptInfo;

            var referenceGuid = new PropertyInfo { DataStructure = info.DataStructure, Name = info.Name + "ID" };
            PropertyHelper.GenerateCodeForType(referenceGuid, codeBuilder, "Guid?");

            DataStructureQueryableCodeGenerator.AddNavigationalProperty(codeBuilder, info.DataStructure, info.Name, info.Referenced.Module.Name + "_" + info.Referenced.Name, info.Name + "ID");

            if (info.DataStructure is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode(
                    string.Format(CultureInfo.InvariantCulture,
@"            foreach(var item in insertedNew)
                if(item.{0}ID != null)
                    item.{0} = _executionContext.NHibernateSession.Load<{1}.{2}>(item.{0}ID);
            foreach(var item in updatedNew)
                if(item.{0}ID != null)
                    item.{0} = _executionContext.NHibernateSession.Load<{1}.{2}>(item.{0}ID);
            foreach(var item in deletedIds)
                if(item.{0}ID != null)
                    item.{0} = _executionContext.NHibernateSession.Load<{1}.{2}>(item.{0}ID);

",
                        info.Name,
                        info.Referenced.Module.Name,
                        info.Referenced.Name),
                    WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.DataStructure));
            }
        }
    }
}
