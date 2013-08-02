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
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.RestGenerator;

namespace Rhetos.Rest.DefaultConcepts
{
    [Export(typeof(IRestGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(FilterByInfo))]
    public class FilterByCodeGenerator : IRestGeneratorPlugin
    {
        private static string CodeSnippet(FilterByInfo info)
        {
            var fullTypeName = info.Parameter;
            if (!fullTypeName.Contains('.'))
                fullTypeName = info.Source.Module.Name + "." + fullTypeName;

            string result = String.Format(
@"Tuple.Create(""{0}"", typeof({0})),
                ", fullTypeName);

            var shortName = TryExtractShortName(fullTypeName);
            if (shortName != null)
                result += String.Format(
@"Tuple.Create(""{0}"", typeof({1})),
                ", shortName, fullTypeName);

            return result;
        }

        private static string TryExtractShortName(string typeName)
        {
            if (typeName.Contains('.'))
            {
                var shortName = typeName.Split('.').Last();
                if (System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(shortName))
                    return shortName;
            }
            return null;
        }
   
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (FilterByInfo)conceptInfo;

            if (DataStructureCodeGenerator.IsTypeSupported(info.Source))
                codeBuilder.InsertCode(CodeSnippet(info), DataStructureCodeGenerator.FilterTypesTag, info.Source);
        }
    }
}
