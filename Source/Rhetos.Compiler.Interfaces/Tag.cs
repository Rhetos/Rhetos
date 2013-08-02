/*
    Copyright (C) 2013 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using System.Globalization;

namespace Rhetos.Compiler
{
    public enum TagType
    { 
        /// <summary>Code can be inserted at the tag position only once.</summary>
        Single,
        /// <summary>Code can be inserted at the tag position multiple times. New code is inserted <b>after</b> the previously inserted code.</summary>
        Appendable,
        /// <summary>Not a tag. Used only for manual format evaluation.</summary>
        [Obsolete("Create a 'private static string SomethingSnippet(SomeConceptInfo info)' with string.Format instead of a Tag the generate code snippet.")]
        CodeSnippet
    };

    public class Tag<T>
    {
        public TagType TagType { get; private set; }
        public string Format { get; private set; }
        public Func<T, string, string> TagFormatter { get; private set; }
        public string NextFormat { get; private set; }
        public string FirstEvaluationContext { get; private set; }
        public string NextEvaluationContext { get; private set; }

        public Tag(TagType tagType, string tagFormat, Func<T, string, string> tagFormatter, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
        {
            if (nextTagFormat != null && new[] { TagType.Single, TagType.CodeSnippet }.Contains(tagType))
                throw new FrameworkException(string.Format(
                    "IncrementalTagFormat is not applicable if TagType is {0}. TagFormat=\"{1}\", IncrementalTagFormat=\"{2}\"",
                    tagType.ToString(), tagFormat, nextTagFormat));

            this.TagType = tagType;
            this.Format = tagFormat;
            this.NextFormat = nextTagFormat;
            this.TagFormatter = tagFormatter;
            this.FirstEvaluationContext = firstEvaluationContext;
            this.NextEvaluationContext = nextEvaluationContext;
        }

        public string Evaluate(T info)
        {
            return TagFormatter(info, Format);
        }

        public static implicit operator string(Tag<T> tag)
        {
            if (tag == null)
                throw new FrameworkException("Cannot use uninitialized (null) tag. Hint: Try reordering static tag members in their parent class.");
            return tag.Format;
        }
    }

    public static class TagCodeBuilder
    {
        public static void InsertCode<T>(this ICodeBuilder codeBuilder, string code, Tag<T> tag, T tagConceptInfo)
        {
            if (tag.TagType == TagType.CodeSnippet)
                throw new FrameworkException(string.Format(
                    "If TagType is {0}, the tag can only be used for manual format evaluation. Use Evaluate() function. TagFormat=\"{1}\"",
                    tag.TagType.ToString(), tag.Format));

            string code1 = code;
            string code2 = code;
            if (!string.IsNullOrEmpty(tag.FirstEvaluationContext))
                code1 = string.Format(CultureInfo.InvariantCulture, tag.FirstEvaluationContext, code);
            if (!string.IsNullOrEmpty(tag.NextEvaluationContext))
                code2 = string.Format(CultureInfo.InvariantCulture, tag.NextEvaluationContext, code);

            string tagPattern = tag.TagFormatter(tagConceptInfo, tag.Format);

            if (tag.TagType == TagType.Single)
                codeBuilder.ReplaceCode(code1, tagPattern);
            else if (tag.NextFormat == null || tag.NextFormat == tag.Format)
                codeBuilder.InsertCode(code1, tagPattern);
            else
            {
                string nextTagPattern = tag.TagFormatter(tagConceptInfo, tag.NextFormat);
                codeBuilder.InsertCode(code1, code2, tagPattern, nextTagPattern);
            }
        }
    }
}
