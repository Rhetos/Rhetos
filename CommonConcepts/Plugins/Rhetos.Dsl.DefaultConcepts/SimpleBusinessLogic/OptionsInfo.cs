using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Aditional options when creating an SQL INDEX appended at the end of generated code for creating an index.
    /// Use options such as WITH and ON.
    /// Don't use INCLUDE or WHERE as a part of these options, as they have their own Concepts.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Options")]
    public class OptionsInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }

        [ConceptKey]
        public string SqlOptions { get; set; }
    }
}
