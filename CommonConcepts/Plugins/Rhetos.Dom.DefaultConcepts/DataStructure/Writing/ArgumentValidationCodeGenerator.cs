using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Dsl;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ArgumentValidationInfo))]
    public class ArgumentValidationCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ArgumentValidationInfo)conceptInfo;
            codeBuilder.InsertCode(GetSnippet(info), WritableOrmDataStructureCodeGenerator.ArgumentValidationTag, info.SaveMethod.Entity);
        }

        private string GetSnippet(ArgumentValidationInfo info)
        {
            return string.Format(
                @"{{ // {0}
                    {1}
                }}

                ",
                info.RuleName, info.CsCodeSnippet.Trim());
        }
    }
}
