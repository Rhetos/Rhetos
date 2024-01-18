using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts.DatabaseWorkarounds
{
    /// <summary>
    /// 
    /// </summary>
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
