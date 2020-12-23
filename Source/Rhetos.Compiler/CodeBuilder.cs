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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Compiler
{
    public class CodeBuilder : ICodeBuilder
    {
        private readonly FastReplacer _code;

        public CodeBuilder(string tagOpen, string tagClose)
        {
            _code =  new FastReplacer(tagOpen, tagClose);
        }

        public void InsertCodeToFile(string code, string path)
        {
            _code.AppendToFile(code, path);
        }

        public void InsertCode(string code)
        {
            _code.Append(code);
        }

        public void InsertCode(string code, string tag)
        {
            InsertCode(code, tag, false);
        }

        public void InsertCode(string code, string tag, bool insertAfterTag)
        {
            CheckTag(code, tag);
            if (insertAfterTag)
                _code.InsertAfter(tag, code, reverse: true);
            else
                _code.InsertBefore(tag, code);
                
        }

        public void InsertCode(string firstCode, string nextCode, string firstTag, string nextTag)
        {
            InsertCode(firstCode, nextCode, firstTag, nextTag, false);
        }

        public void InsertCode(string firstCode, string nextCode, string firstTag, string nextTag, bool insertAfterTag)
        {
            CheckTags(firstCode + " /OR/ " + nextCode, firstTag, nextTag);
            if (insertAfterTag)
                _code.InsertAfter(nextTag, nextCode, reverse: true);
            else
                _code.InsertBefore(nextTag, nextCode);
            _code.Replace(firstTag, firstCode + nextTag);
        }

        public void ReplaceCode(string code, string tag)
        {
            CheckTag(code, tag);
            _code.Replace(tag, code);
        }

        private void CheckTag(string code, string tag)
        {
            if (!_code.Contains(tag))
                throw new FrameworkException($"Generated script does not contain tag \"{tag}\"." +
                    $" Error occurred while inserting code \"{code.Limit(200, "...")}\".");
        }

        private void CheckTags(string code, string tag1, string tag2)
        {
            if (!_code.Contains(tag1) && !_code.Contains(tag2))
                throw new FrameworkException($"Generated script does not contain tag \"{tag1}\" nor tag \"{tag2}\"." +
                    $" Error occurred while inserting code \"{code.Limit(200, "...")}\".");
        }

        public bool TagExists(string tag)
        {
            return _code.Contains(tag);
        }

        [Obsolete("Use GenerateCode() instead. The property is misleading because it does not cache the generated string.")]
        public string GeneratedCode => GenerateCode();

        public string GenerateCode() => _code.ToString();

        public IDictionary<string, string> GeneratedCodeByFile => _code.GetPaths().ToDictionary(path => path, path => _code.ToString(path));
    }
}
