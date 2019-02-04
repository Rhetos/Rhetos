using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ArgumentValidation")]
    public class ArgumentValidationInfo : IConceptInfo
    {
        [ConceptKey]
        public SaveMethodInfo SaveMethod { get; set; }

        /// <summary>
        /// Unique name of this business rule.
        /// </summary>
        [ConceptKey]
        public string RuleName { get; set; }

        /// <summary>
        /// Available variables in this context:
        ///     _executionContext,
        ///     checkUserPermissions (whether the Save command is called directly by client through a web API)
        ///     inserted (array of new items),
        ///     updated (array of new items).
        ///     See <see cref="WritableOrmDataStructureCodeGenerator.ArgumentValidationTag">WritableOrmDataStructureCodeGenerator.OnSaveTag2</see> for more info.
        /// </summary>
        public string CsCodeSnippet { get; set; }
    }
}
