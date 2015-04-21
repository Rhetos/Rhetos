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
using System.Globalization;
using System.ComponentModel.Composition;
using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ImplementsQueryableInterfacePropertyInfo))]
    public class ImplementsQueryableInterfacePropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ImplementsQueryableInterfacePropertyInfo)conceptInfo;

            codeBuilder.InsertCode(ExplicitPropertyImplemenetation(info), DataStructureQueryableCodeGenerator.MembersTag, info.Property.DataStructure);
        }

        protected static string ExplicitPropertyImplemenetation(ImplementsQueryableInterfacePropertyInfo info)
        {
            return string.Format("{0} {1}.{2} {{ get {{ return {2}; }} }}\r\n        ",
                info.PropertyInterfaceTypeName,
                info.ImplementsQueryableInterface.GetInterfaceType().FullName,
                info.Property.Name);
        }
    }
}
