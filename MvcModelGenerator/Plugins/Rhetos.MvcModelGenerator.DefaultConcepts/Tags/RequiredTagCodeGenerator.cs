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
    [ExportMetadata(MefProvider.Implements, typeof(RequiredPropertyInfo))]
    public class RequiredTagCodeGenerator : IMvcModelGeneratorPlugin
    {
        private static string ImplementationCodeSnippet(RequiredPropertyInfo info)
        {
            return string.Format(@"[Required]
        ");
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            if (conceptInfo is RequiredPropertyInfo)
            {
                RequiredPropertyInfo info = (RequiredPropertyInfo)conceptInfo;

                if (DataStructureCodeGenerator.IsTypeSupported(info.Property.DataStructure))
                {
                    codeBuilder.InsertCode(ImplementationCodeSnippet((RequiredPropertyInfo)info), MvcPropertyHelper.AttributeTag, info.Property);
                }
            }
        }
    }
}