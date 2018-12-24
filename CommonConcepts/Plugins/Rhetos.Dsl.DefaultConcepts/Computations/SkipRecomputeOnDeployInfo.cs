using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SkipRecomputeOnDeploy")]
    public class SkipRecomputeOnDeployInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        public string GetKey()
        {
            return EntityComputedFrom.Source.ToString() + "/" + EntityComputedFrom.Target.ToString();
        }
    }
}
