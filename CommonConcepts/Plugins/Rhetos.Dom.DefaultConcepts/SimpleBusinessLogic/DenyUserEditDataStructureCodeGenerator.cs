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
    [ExportMetadata(MefProvider.Implements, typeof(DenyUserEditDataStructureInfo))]
    public class DenyUserEditDataStructureCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DenyUserEditDataStructureInfo)conceptInfo;
            codeBuilder.InsertCode(CheckChangesSnippet(info), WritableOrmDataStructureCodeGenerator.ArgumentValidationTag, info.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string CheckChangesSnippet(DenyUserEditDataStructureInfo info)
        {
            return string.Format(
            @"if (checkUserPermissions)
                throw new Rhetos.UserException(
                    ""It is not allowed to directly modify {{0}}."", new[] {{ ""{0}.{1}"" }}, null, null);
            ",
                info.DataStructure.Module.Name,
                info.DataStructure.Name);
        }
    }
}
