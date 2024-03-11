using System;

namespace Rhetos.Dsl
{
#pragma warning disable CA1032 // Implement standard exception constructors. There is no need for standard constructor because if 'IConceptInfo' is not provided then the DslSyntaxException type should be used instead.
    /// <remarks>
    /// This exception is a helper for generating <see cref="DslSyntaxException"/> with <see cref="IConceptInfo"/>.
    /// The base <see cref="DslSyntaxException"/> type is a part for Rhetos DSL parser, which does not reference the <see cref="IConceptInfo"/> type.
    /// This is the reason why <see cref="DslConceptSyntaxException"/> is implemented as a separate exception type in a separate assembly.
    /// </remarks>
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
#pragma warning restore CA1032 // Implement standard exception constructors
}
