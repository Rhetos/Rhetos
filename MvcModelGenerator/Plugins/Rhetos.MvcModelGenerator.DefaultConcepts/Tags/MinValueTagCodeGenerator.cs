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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Xml;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.MvcModelGenerator;

namespace Rhetos.MvcModelGenerator.DefaultConcepts
{
    [Export(typeof(IMvcModelGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(MinValueInfo))]
    public class MinValueTagCodeGenerator : IMvcModelGeneratorPlugin
    {
        private static string ImplementationCodeSnippet(MinValueInfo info)
        {
            var typeRange = (info.Property is IntegerPropertyInfo) ? "Integer" :
                (info.Property is MoneyPropertyInfo || info.Property is DecimalPropertyInfo) ? "Decimal" :
                (info.Property is DatePropertyInfo || info.Property is DateTimePropertyInfo) ? "Date" : "";

            return string.Format(@"[Rhetos.Mvc.MinValue{0}(MinValue = ""{1}"", ErrorMessage = ""Value for {2} must be greater than or equal to {1}."")]
        ", typeRange, info.Value.ToString(), info.Property.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            if (conceptInfo is MinValueInfo)
            {
                MinValueInfo info = (MinValueInfo)conceptInfo;

                if (DataStructureCodeGenerator.IsTypeSupported(info.Property.DataStructure))
                {
                    codeBuilder.InsertCode(ImplementationCodeSnippet((MinValueInfo)info), MvcPropertyHelper.AttributeTag, info.Property);
                }
            }
        }
    }
}