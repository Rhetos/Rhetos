using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rhetos.Compiler
{
    public class CsTag<T> : Tag2<T>
        where T : IConceptInfo
    {
        public CsTag(string key, TagType tagType = TagType.Appendable, string firstEvaluationContext = null, string nextEvaluationContext = null)
            : base(key, tagType, firstEvaluationContext, nextEvaluationContext, "/*", "*/")
        {
        }

        public static implicit operator CsTag<T>(string key)
        {
            if (key == null)
                throw new FrameworkException("Cannot create CsTag, the 'key' argument value is null. Hint: Try reordering static tag members in their parent class.");
            return new CsTag<T>(key);
        }
    }
}
