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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InvalidDataMessageFunctionInfo))]
    public class InvalidDataMessageFunctionCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (InvalidDataMessageFunctionInfo)conceptInfo;

            // Using underscore in variable name to avoid name clashes with custom injected code.
            string setMessages =
            @"
            Func<IEnumerable<Guid>, IEnumerable<InvalidDataMessage>> invalidData_Func = " + info.MessageFunction + @";
            return invalidData_Func(invalidData_Ids);";
            codeBuilder.InsertCode(setMessages, InvalidDataCodeGenerator.CustomValidationResultTag, info.InvalidData);
        }
    }
}
