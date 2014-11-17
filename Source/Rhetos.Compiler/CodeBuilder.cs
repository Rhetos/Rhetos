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
using System.Linq;
using System.Reflection;
using Rhetos.Utilities;

namespace Rhetos.Compiler
{
    public class CodeBuilder : ICodeBuilder, IAssemblySource
    {
        private readonly FastReplacer _code;
        private readonly HashSet<string> _references = new HashSet<string>();

        public CodeBuilder(string tagOpen, string tagClose)
        {
            _code =  new FastReplacer(tagOpen, tagClose);
        }

        private void AddReferenceByLocation(string assemblyLocation)
        {
            if (!_references.Contains(assemblyLocation))
                _references.Add(assemblyLocation);
        }

        /// <summary>
        /// Use AddReferencesFromDependency to safely add reference to exact assembly, instead of using dll's short name to guess assembly version that should be used.
        /// </summary>
        public void AddReference(string shortName)
        {
            AddReferenceByLocation(shortName);
        }

        public void AddReferencesFromDependency(Type type)
        {
            AddReferenceByLocation(type.Assembly.Location);
            while (type.BaseType != null && type.BaseType != typeof(object))
            {
                type = type.BaseType;
                AddReferenceByLocation(type.Assembly.Location);
            }
        }

        public void InsertCode(string code)
        {
            _code.Append(code + Environment.NewLine);
        }

        public void InsertCode(string code, string tag)
        {
            InsertCode(code, tag, false);
        }

        public void InsertCode(string code, string tag, bool insertAfterTag)
        {
            CheckTag(code, tag);
            if (insertAfterTag)
                _code.InsertAfter(tag, code);
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
                _code.InsertAfter(nextTag, nextCode);
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
                throw new FrameworkException(string.Format(
                    "Generated script does not contain tag \"{0}\". Error occured while inserting code \"{1}\".",
                    tag, code.Substring(0, Math.Min(200, code.Length))));
        }

        private void CheckTags(string code, string tag1, string tag2)
        {
            if (!_code.Contains(tag1) && !_code.Contains(tag2))
                throw new FrameworkException(string.Format(
                    "Generated script does not contain tag \"{0}\" nor tag \"{1}\". Error occured while inserting code \"{2}\".",
                    tag1, tag2, code.Substring(0, Math.Min(200, code.Length))));
        }

        public bool TagExists(string tag)
        {
            return _code.Contains(tag);
        }

        public string GeneratedCode
        {
            get
            {
                string referencesList = string.Join("", RegisteredReferences.Select(reference => "// Reference: " + reference + Environment.NewLine));
                return referencesList + Environment.NewLine + _code;
            }
        }

        public IEnumerable<string> RegisteredReferences
        {
            get { return _references; }
        }
    }
}
