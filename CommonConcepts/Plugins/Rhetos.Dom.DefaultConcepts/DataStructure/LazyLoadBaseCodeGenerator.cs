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
using System.Text;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LazyLoadBaseInfo))]
    public class LazyLoadBaseCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LazyLoadBaseInfo)conceptInfo;

            var getterSnippet = string.Format(
                @"if (_context != null)
                    return ID == null ? null : _context.Repository.{0}.{1}.Query().Where(item => item.ID == ID).SingleOrDefault();
                ",
                info.Extends.Base.Module.Name,
                info.Extends.Base.Name);

            codeBuilder.InsertCode(getterSnippet, DataStructureQueryableCodeGenerator.PropertyGetterTag(info.Extends.Extension, "Base"));

            var setterSnippet =
                @"if (value != null)
                    ID = value.ID;
                else
                    ID = default(Guid);
				return;
                ";

            codeBuilder.InsertCode(setterSnippet, DataStructureQueryableCodeGenerator.PropertySetterTag(info.Extends.Extension, "Base"));
        }
    }
}
