using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("OldDataLoaded")]
    public class OldDataLoadedInfo : IConceptInfo
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
        ///     inserted (array of new items),
        ///     updated (array of new items).
        /// If LoadOldItems concept is used, there are also available:
        ///     updatedOld (array of old items),
        ///     deletedOld (array of old items).
        ///     See <see cref="WritableOrmDataStructureCodeGenerator.OldDataLoadedTag">WritableOrmDataStructureCodeGenerator.OnSaveTag1</see> for more info.
        /// </summary>
        public string CsCodeSnippet { get; set; }
    }
}
