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
using Rhetos.Utilities;
using System.Globalization;

namespace Rhetos.Dom.DefaultConcepts.SimpleBusinessLogic
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueMultiplePropertyInfo))]
    public class UniqueMultiplePropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UniqueMultiplePropertyInfo)conceptInfo;

            if (UniqueMultiplePropertiesCodeGenerator.ImplementInObjectModel(info.Unique))
            {
                var column = GetColumnName(info.Property);

                codeBuilder.InsertCode(column, UniqueMultiplePropertiesCodeGenerator.ColumnListTag, info.Unique);

                codeBuilder.InsertCode("doubles." + column + " = source." + column + " OR doubles." + column + " IS NULL AND source." + column + " IS NULL",
                    UniqueMultiplePropertiesCodeGenerator.ColumnJoinTag, info.Unique);

                codeBuilder.InsertCode("\"" + info.Property.Name + "\"", UniqueMultiplePropertiesCodeGenerator.PropertyListTag, info.Unique);

                var invalidProperty = (info.Property is ReferencePropertyInfo) ? "invalidItem." + info.Property.Name + "ID" : "invalidItem." + info.Property.Name;
                var reportInvalidValue = string.Format("({0} != null ? {0}.ToString() : \"<null>\")", invalidProperty);
                codeBuilder.InsertCode(reportInvalidValue, UniqueMultiplePropertiesCodeGenerator.PropertyValuesTag, info.Unique);
            }
        }
        
        private static string GetColumnName(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return SqlUtility.Identifier(property.Name + "ID");
            return SqlUtility.Identifier(property.Name);
        }
    }
}
