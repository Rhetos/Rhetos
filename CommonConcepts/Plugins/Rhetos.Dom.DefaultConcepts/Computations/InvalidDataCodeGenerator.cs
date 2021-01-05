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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InvalidDataInfo))]
    public class InvalidDataCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<InvalidDataInfo> ErrorMetadataTag = "ErrorMetadata";
        public static readonly CsTag<InvalidDataInfo> CustomValidationResultTag = new CsTag<InvalidDataInfo>("CustomValidationResult", TagType.Reverse);

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
            IDictionary<string, object> metadata = new Dictionary<string, object>();
            {ErrorMetadataTag.Evaluate(info)}
            {CustomValidationResultTag.Evaluate(info)}
        }}

        ";
            codeBuilder.InsertCode(errorMessageMethod, RepositoryHelper.RepositoryMembers, info.Source);

            // HACK: Using IDslModel as a cleaner alternative to ConceptMetadata (which is not saved with DslModel).
            // We could remove InvalidDataMessageInfo and InvalidDataAllowSaveInfo concepts,
            // add a base validation concept for InvalidData that does not have these features (default validation result and DenySave),
            // and make InvalidData a macro that adds the features, but the result would not be fully backward compatible.
            bool hasCustomValidationMessage = _dslModel.FindByKey($"{nameof(InvalidDataMessageInfo)} {info.GetKeyProperties()}") != null;

            if (!hasCustomValidationMessage)
            {
                string defaultValidationResult =
            $@"
            return invalidData_Ids.Select(id => new InvalidDataMessage
            {{
                ID = id,
                Message = {CsUtility.QuotedString(info.ErrorMessage)},
                Metadata = metadata
            }});";
                codeBuilder.InsertCode(defaultValidationResult, CustomValidationResultTag, info);
            }

            codeBuilder.InsertCode(
                "metadata[\"Validation\"] = " + CsUtility.QuotedString(info.FilterType) + ";\r\n            ",
                ErrorMetadataTag, info);

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
