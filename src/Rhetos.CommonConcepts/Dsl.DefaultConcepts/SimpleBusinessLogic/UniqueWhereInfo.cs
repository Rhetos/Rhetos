using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Filter used when creating a unique index.
    /// Check official documentation for adding conditions when creating an index.
    /// https://learn.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql?view=sql-server-ver16#syntax
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Where")]
    public class UniqueWhereInfo : SqlIndexWhereInfo, IAlternativeInitializationConcept
    {
        public UniquePropertyInfo Unique { get; set; }
        public string UniqueSqlFilter { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { 
                nameof(SqlIndex),
                nameof(SqlFilter)
            };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            SqlFilter = UniqueSqlFilter;
            SqlIndex = new SqlIndexMultipleInfo
            {
                DataStructure = Unique.Property.DataStructure,
                PropertyNames = Unique.Property.Name
            };
            createdConcepts = null;
        }
    }
}
