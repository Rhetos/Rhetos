using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rhetos.Compiler
{
    public class Tag2<T>
        where T : IConceptInfo
    {
        public string Format { get; protected set; }
        public string NextFormat { get; protected set; }
        public TagType TagType { get; protected set; }
        public string FirstEvaluationContext { get; protected set; }
        public string NextEvaluationContext { get; protected set; }
        public string TagOpen { get; protected set; }
        public string TagClose { get; protected set; }

        public Tag2(string key, TagType tagType, string firstEvaluationContext, string nextEvaluationContext, string tagOpen, string tagClose)
        {
            if (nextEvaluationContext != null && tagType == TagType.Single)
                throw new FrameworkException(string.Format(
                    "Incremental formatting (using nextEvaluationContext) is not applicable if TagType is {0}. Concept={1}, Key=\"{2}\", nextEvaluationContext=\"{2}\"",
                    tagType, typeof(T).Name, key, nextEvaluationContext));
           
            TagType = tagType;
            FirstEvaluationContext = firstEvaluationContext;
            NextEvaluationContext = nextEvaluationContext;

            TagOpen = tagOpen;
            TagClose = tagClose;

            Format = TagOpen + typeof(T).Name + " " + key + " {0}" + TagClose;
            if (nextEvaluationContext != null)
                NextFormat = TagOpen + "Next " + typeof(T).Name + " " + key + " {0}" + TagClose;
        }

        public string Evaluate(T conceptInfo)
        {
            return string.Format(CultureInfo.InvariantCulture, Format, conceptInfo.GetKeyProperties());
        }
    }

    public static class Tag2CodeBuilder
    {
        public static void InsertCode<T>(this ICodeBuilder codeBuilder, string code, Tag2<T> tag, T conceptInfo)
            where T : IConceptInfo
        {
            string code1 = code;
            string code2 = code;
            if (!string.IsNullOrEmpty(tag.FirstEvaluationContext))
                code1 = string.Format(CultureInfo.InvariantCulture, tag.FirstEvaluationContext, code);
            if (!string.IsNullOrEmpty(tag.NextEvaluationContext))
                code2 = string.Format(CultureInfo.InvariantCulture, tag.NextEvaluationContext, code);

            var conceptInfoKeyProperties = conceptInfo.GetKeyProperties();
            string tagPattern = string.Format(CultureInfo.InvariantCulture, tag.Format, conceptInfoKeyProperties);

            if (tag.TagType == TagType.Single)
                codeBuilder.ReplaceCode(code1, tagPattern);
            else if (tag.NextFormat == null)
                codeBuilder.InsertCode(code1, tagPattern);
            else
            {
                string nextTagPattern = string.Format(CultureInfo.InvariantCulture, tag.NextFormat, conceptInfoKeyProperties);
                codeBuilder.InsertCode(code1, code2, tagPattern, nextTagPattern);
            }
        }
    }
}
