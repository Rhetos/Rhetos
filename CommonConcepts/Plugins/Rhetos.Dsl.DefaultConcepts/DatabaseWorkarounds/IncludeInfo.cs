using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts.DatabaseWorkarounds
{
    /// <summary>
    /// Adds the INCLUDE columns (nonkey) to the index.
    /// The <see cref="Columns"/> parameter is a list of properties separated by space.
    /// It may also contain other database columns that are not created as Rhetos properties, such as ID.
    /// </summary>
    /// <remarks>
    /// If including a column that is not recognized as a Rhetos property (for example a column created by custom SqlObject),
    /// then add a SqlDependsOnSqlObject on the SQL index (typically on SqlIndexMultiple) with a reference
    /// to the SqlObject that created that column.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Include")]
    public class IncludeInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }

        [ConceptKey]
        public string Columns { get; set; }
    }
}
