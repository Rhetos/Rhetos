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
    public class UniqueWhereInfo : IAlternativeInitializationConcept
    {
        [ConceptKey]
        public UniquePropertyInfo Unique { get; set; }

        [ConceptKey]
        public string SqlFilter { get; set; }

        public SqlIndexMultipleInfo Dependency_SqlIndex { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Dependency_SqlIndex) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_SqlIndex = new SqlIndexMultipleInfo
            {
                DataStructure = Unique.Property.DataStructure,
                PropertyNames = Unique.Property.Name
            };
            createdConcepts = null;
        }
    }
}
