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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(MaxLengthInfo))]
    [ExportMetadata(MefProvider.ClassType, typeof(MaxLengthCodeGenerator))]
    public class MaxLengthCodeGenerator : IConceptCodeGenerator
    {
        private const string CodeSnippet =
@"            foreach(var item in insertedNew)
                if(!String.IsNullOrEmpty(item.{0}))
                    if(item.{0}.Length > {1})
                        throw new Rhetos.UserException(""Maximum length of {0} is {1} characters."");

              foreach(var item in updatedNew)
                if(!String.IsNullOrEmpty(item.{0}))
                    if(item.{0}.Length > {1})
                        throw new Rhetos.UserException(""Maximum length of {0} is {1} characters."");
";
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (MaxLengthInfo) conceptInfo;

            codeBuilder.InsertCode(String.Format(CodeSnippet, info.Property.Name, info.Length), WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }
    }
}
