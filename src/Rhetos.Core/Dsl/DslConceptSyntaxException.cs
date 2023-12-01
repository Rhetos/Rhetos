using System;

namespace Rhetos.Dsl
{
    [Serializable]
    public class DslConceptSyntaxException : DslSyntaxException
    {
        public DslConceptSyntaxException(IConceptInfo concept, string additionalMessage)
            : base(concept.GetUserDescription() + ": " + additionalMessage)
        {
        }

        public DslConceptSyntaxException(IConceptInfo concept, string additionalMessage, Exception inner)
            : base(concept.GetUserDescription() + ": " + additionalMessage, inner)
        {
        }

        public DslConceptSyntaxException()
        {
        }
    }
}
