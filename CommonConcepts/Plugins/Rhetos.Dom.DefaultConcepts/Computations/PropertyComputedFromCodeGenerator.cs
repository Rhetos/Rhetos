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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyComputedFromInfo))]
    public class PropertyComputedFromCodeGenerator : IConceptCodeGenerator
    {
        private static string CompareValuePropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(
                "if (x.{0} == null && y.{0} != null || x.{0} != null && !x.{0}.Equals(y.{0})) return false;\r\n                    ",
                GetPropertyName(info.Target));
        }

        private static string ClonePropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(
                "{0} = sourceItem.{1},\r\n                ",
                GetPropertyName(info.Target), GetPropertyName(info.Source));
        }

        private static string AssignPropertySnippet(PropertyComputedFromInfo info)
        {
            return string.Format(
                "destination.{0} = source.{0};\r\n                    ",
                GetPropertyName(info.Target));
        }

        private static string GetPropertyName(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return property.Name + "ID";
            else
                return property.Name;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PropertyComputedFromInfo)conceptInfo;
            codeBuilder.InsertCode(CompareValuePropertySnippet(info), EntityComputedFromCodeGenerator.CompareValuePropertyTag, info.Dependency_EntityComputedFrom);
            codeBuilder.InsertCode(ClonePropertySnippet(info), EntityComputedFromCodeGenerator.ClonePropertyTag, info.Dependency_EntityComputedFrom);
            codeBuilder.InsertCode(AssignPropertySnippet(info), EntityComputedFromCodeGenerator.AssignPropertyTag, info.Dependency_EntityComputedFrom);
        }
    }
}
