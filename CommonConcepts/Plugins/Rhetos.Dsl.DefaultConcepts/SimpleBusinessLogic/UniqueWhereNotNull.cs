using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Filter used when creating a unique index that disregards NULL values.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("UniqueWhereNotNull")]
    public class UniqueWhereNotNullInfo : UniquePropertyInfo
    {
    }
}
