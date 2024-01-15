using Rhetos.DatabaseGenerator.DefaultConcepts;
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

    [Export(typeof(IConceptMacro))]
    public class UniqueWhereNotNullMacro : IConceptMacro<UniqueWhereNotNullInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public UniqueWhereNotNullMacro(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueWhereNotNullInfo conceptInfo, IDslModel existingConcepts)
        {
            return new IConceptInfo[] {
                new SqlIndexWhereInfo {
                    SqlIndex = conceptInfo.Dependency_SqlIndex,
                    SqlFilter = _conceptMetadata.GetColumnName(conceptInfo.Property) + " IS NOT NULL"
                }
            };
        }
    }
}
