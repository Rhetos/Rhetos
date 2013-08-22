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
using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rhetos.Compiler
{
    public class CsTag<T> : Tag<T>
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
