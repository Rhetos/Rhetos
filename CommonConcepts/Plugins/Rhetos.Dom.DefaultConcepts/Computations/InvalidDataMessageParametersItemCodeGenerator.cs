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
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InvalidDataMessageParametersItemInfo))]
    public class InvalidDataMessageParametersItemCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (InvalidDataMessageParametersItemInfo)conceptInfo;

            int maxParameter = FindMaxParameter(info.InvalidData.ErrorMessage);
            string parametersArray = "new object[] { "
                + string.Join(", ", Enumerable.Range(0, maxParameter + 1).Select(p => "parameters.P" + p))
                + " }";

            string setMessages =
            @"return this.Query(invalidData_Ids)
                .Select(" + info.MessageParameters + @")
                .ToList()
                .Select(parameters => new InvalidDataMessage
                {
                    ID = parameters.ID,
                    Message = invalidData_Description,
                    MessageParameters = " + parametersArray + @",
                    Metadata = metadata
                });
            // ";
            codeBuilder.InsertCode(setMessages, InvalidDataCodeGenerator.OverrideUserMessagesTag, info.InvalidData);
        }

        private int FindMaxParameter(string messageFormat)
        {
            var parameters = _extractParameters.Matches(messageFormat)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Select(param => int.Parse(param))
                .ToList();

            if (parameters.Count() > 0)
                return parameters.Max();
            else
                return -1;
        }

        private static readonly Regex _extractParameters = new Regex(@"\{(\d+)\}", RegexOptions.Singleline);
    }
}
