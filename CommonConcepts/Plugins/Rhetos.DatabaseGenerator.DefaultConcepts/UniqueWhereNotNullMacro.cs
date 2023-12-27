using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
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
            return new[] {
                new UniqueWhereInfo {
                    Unique = conceptInfo,
                    SqlFilter = _conceptMetadata.GetColumnName(conceptInfo.Property) + " IS NOT NULL"
                }
            };
        }
    }
}
