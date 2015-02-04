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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LockItemsLockPropertyInfo))]
    public class LockItemsLockPropertyCodeGenerator : IConceptCodeGenerator
    {

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LockItemsLockPropertyInfo)conceptInfo;
            codeBuilder.InsertCode(ComparePropertySnippet(info), LockItemsExceptPropertiesCodeGenerator.ComparePropertyTag, info.Lock);
        }

        private static string ComparePropertySnippet(LockItemsLockPropertyInfo conceptInfo)
        {
            string propertyName = conceptInfo.Property.Name;
            if (conceptInfo.Property is ReferencePropertyInfo)
                propertyName += "ID";

            return string.Format(
                "\r\n                    || (i.{0} == null && j.{0} != null || i.{0} != null && !i.{0}.Equals(j.{0}))",
                propertyName);
        }
    }
}
