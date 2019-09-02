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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InvalidDataInfo))]
    public class InvalidDataCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<InvalidDataInfo> ErrorMetadataTag = "ErrorMetadata";
        public static readonly CsTag<InvalidDataInfo> OverrideUserMessagesTag = "OverrideUserMessages";

        private readonly IDslModel _dslModel;

        public InvalidDataCodeGenerator(IDslModel dslModel)
        {
            _dslModel = dslModel;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (InvalidDataInfo)conceptInfo;

            // Using nonstandard naming of variables to avoid name clashes with injected code.
            string errorMessageMethod =
        $@"public IEnumerable<InvalidDataMessage> {info.GetErrorMessageMethodName()}(IEnumerable<Guid> invalidData_Ids)
        {{
            const string invalidData_Description = {CsUtility.QuotedString(info.ErrorMessage)};
            IDictionary<string, object> metadata = new Dictionary<string, object>();
            {ErrorMetadataTag.Evaluate(info)}
            {OverrideUserMessagesTag.Evaluate(info)} return invalidData_Ids.Select(id => new InvalidDataMessage {{ ID = id, Message = invalidData_Description, Metadata = metadata }});
        }}

        ";
            codeBuilder.InsertCode(errorMessageMethod, RepositoryHelper.RepositoryMembers, info.Source);
            codeBuilder.AddReferencesFromDependency(typeof(InvalidDataMessage));

            codeBuilder.InsertCode(
                "metadata[\"Validation\"] = " + CsUtility.QuotedString(info.FilterType) + ";\r\n            ",
                ErrorMetadataTag, info);

            // HACK: IDslModel should not be used in code generator.
            // We should remove AllowSave concept,
            // add a base validation concept for InvalidData that does not block saving,
            // add DenySave concept that referenced the base validation and blocks saving,
            // and make InvalidData a macro that adds DenySave.
            bool allowSave = _dslModel.FindByKey($"{nameof(InvalidDataAllowSaveInfo)} {info.GetKeyProperties()}") != null;

            string validationSnippet =
            $@"if ({(allowSave ? "!" : "")}onSave)
            {{
                var errorIds = this.Filter(this.Query(ids), new {info.FilterType}()).Select(item => item.ID).ToArray();
                if (errorIds.Count() > 0)
                    foreach (var error in {info.GetErrorMessageMethodName()}(errorIds))
                        yield return error;
            }}
            ";
            codeBuilder.InsertCode(validationSnippet, WritableOrmDataStructureCodeGenerator.OnSaveValidateTag, info.Source);
        }
    }
}
