/*
    Copyright (C) 2014 Omega software d.o.o.

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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Dsl;

namespace Rhetos.Compiler
{
    public enum TagType
    { 
        /// <summary>
        /// Code can be inserted at the tag position only once.
        /// </summary>
        Single,
        /// <summary>
        /// Code can be inserted at the tag position multiple times.
        /// New code is inserted <b>after</b> the previously inserted code.
        /// </summary>
        Appendable,
        /// <summary>
        /// Code can be inserted at the tag position multiple times.
        /// New code is inserted <b>before</b> the previously inserted code,
        /// so that multiple insertions at the same tag will result
        /// in reversed order of the generated code.
        /// </summary>
        Reverse
    };

    public class Tag<T>
        where T : IConceptInfo
    {
        public string Key { get; protected set; }
        public TagType TagType { get; protected set; }
        public string FirstEvaluationContext { get; protected set; }
        public string NextEvaluationContext { get; protected set; }
        public string TagOpen { get; protected set; }
        public string TagClose { get; protected set; }

        public string Format { get; protected set; }
        public string NextFormat { get; protected set; }

        public Tag(string key, TagType tagType, string firstEvaluationContext, string nextEvaluationContext, string tagOpen, string tagClose)
        {
            Key = key;
            TagType = tagType;
            FirstEvaluationContext = firstEvaluationContext;
            NextEvaluationContext = nextEvaluationContext;
            TagOpen = tagOpen;
            TagClose = tagClose;

            Format = TagOpen + typeof(T).Name + " " + key + " {0}" + TagClose;
            if (nextEvaluationContext != null)
                NextFormat = TagOpen + "Next " + typeof(T).Name + " " + key + " {0}" + TagClose;

            if (nextEvaluationContext != null && tagType == TagType.Single)
                throw new FrameworkException(string.Format(
                    "Incremental formatting (using nextEvaluationContext) is not applicable if TagType is {0}. Invalid {1} for concept {2}, key=\"{3}\", nextEvaluationContext=\"{4}\".",
                    TagType, GetType().Name, typeof(T).Name, Key, NextEvaluationContext));
        }

        public string Evaluate(T conceptInfo)
        {
            return string.Format(CultureInfo.InvariantCulture, Format, conceptInfo.GetKeyProperties());
        }

        public override string ToString()
        {
            throw new FrameworkException($"Tag must be evaluated using Evaluate() function instead of being used directly as a string." +
                $" Invalid use of {GetType().Name} for concept {typeof(T).Name}, key=\"{Key}\", nextEvaluationContext=\"{NextEvaluationContext}\".");
        }
    }

    public static class TagCodeBuilder
    {
        public static void InsertCode<T>(this ICodeBuilder codeBuilder, string code, Tag<T> tag, T conceptInfo)
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
                codeBuilder.InsertCode(code1, tagPattern, tag.TagType == TagType.Reverse);
            else
            {
                string nextTagPattern = string.Format(CultureInfo.InvariantCulture, tag.NextFormat, conceptInfoKeyProperties);
                codeBuilder.InsertCode(code1, code2, tagPattern, nextTagPattern, tag.TagType == TagType.Reverse);
            }
        }
    }
}
