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
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityHistoryPropertyInfo))]
    public class EntityHistoryPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityHistoryPropertyInfo)conceptInfo;

            string csPropertyName = info.Property is ReferencePropertyInfo
                ? info.Property.Name + "ID"
                : info.Property.Name;

            codeBuilder.InsertCode(string.Format(",\r\n							    {0} = olditem.{0}", csPropertyName),
                EntityHistoryCodeGenerator.ClonePropertiesTag, info.Dependency_EntityHistory);
            codeBuilder.InsertCode(string.Format("\r\n                        ret.{0} = item.{0};", csPropertyName),
                EntityHistoryInfo.ClonePropertiesTag, info.Dependency_EntityHistory);
        }
    }
}
