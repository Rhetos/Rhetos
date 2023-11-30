﻿/*
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
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;
using System.Reflection;
using System.IO;
using Rhetos.Utilities;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;
using Rhetos.Dom;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(BrowseFromPropertyInfo))]
    public class BrowsePropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (BrowseFromPropertyInfo)conceptInfo;
            var browse = (BrowseDataStructureInfo)info.PropertyInfo.DataStructure;

            codeBuilder.InsertCode(
                 string.Format("{0} = item.{1},\r\n                    ", info.PropertyInfo.Name, info.Path),
                 BrowseDataStructureCodeGenerator.BrowsePropertiesTag,
                 browse);

            if (info.PropertyInfo is ReferencePropertyInfo)
                codeBuilder.InsertCode(
                 string.Format("{0}ID = item.{1}ID,\r\n                    ", info.PropertyInfo.Name, info.Path),
                 BrowseDataStructureCodeGenerator.BrowsePropertiesTag,
                 browse);
        }
    }
}