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
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyCodeGenerator : IConceptCodeGenerator
    {
        public class ReferenceTag : Tag<ReferencePropertyInfo>
        {
            public ReferenceTag(TagType tagType, string tagFormat, string nextTagFormat = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name, info.Referenced.Module.Name, info.Referenced.Name), nextTagFormat)
            { }
        }

        protected static readonly ReferenceTag ReferenceIDGet = new ReferenceTag(TagType.CodeSnippet,
@"
                if ({2} != null)
                    return {2}.ID;
                return null;
");

        protected static readonly ReferenceTag ReferenceIDSet = new ReferenceTag(TagType.CodeSnippet,
@"
                if(value == null)
                    {2} = null;
                else
                    {2} = new {3}.{4}{{ ID = value.Value }};
");

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ReferencePropertyInfo info = (ReferencePropertyInfo)conceptInfo;
            PropertyHelper.GenerateCodeForType(info, codeBuilder, info.Referenced.Module.Name + "." + info.Referenced.Name, false);

            PropertyInfo referenceIDInfo = new PropertyInfo { DataStructure = info.DataStructure, Name = info.Name + "ID" };
            PropertyHelper.GenerateCodeForType(referenceIDInfo, codeBuilder, "Guid?", true, false);
            codeBuilder.InsertCode(ReferenceIDGet.Evaluate(info), PropertyHelper.BeforeGetPropertyTag, referenceIDInfo);
            codeBuilder.InsertCode(ReferenceIDSet.Evaluate(info), PropertyHelper.BeforeSetPropertyTag, referenceIDInfo);

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
                                  info.Name, info.Referenced.Module.Name, info.Referenced.Name),
                    WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.DataStructure));
            }
        }
    }
}
