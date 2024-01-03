using System;
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
    public class SqlIndexWhereInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }

        public string SqlFilter { get; set; }
    }
}
