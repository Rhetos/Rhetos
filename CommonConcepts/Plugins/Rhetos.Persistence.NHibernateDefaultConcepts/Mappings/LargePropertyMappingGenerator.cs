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
using Rhetos.Persistence;
using Rhetos.Compiler;
using Rhetos.Persistence.NHibernate;
using Rhetos.Utilities;

namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(IConceptMappingCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    public class LargePropertyMappingGenerator : IConceptMappingCodeGenerator
    {
        const int MaxBlobSize = 1024*1024*1024; // If not defined, NHibernate would default to 8000.

        private static string CodeSnippet(PropertyInfo info)
        {
            return string.Format(
@"        <property {0} {1}>
            <column name=""{2}"" length=""{3}"" />
        </property>
",
                NhUtility.PropertyAndColumnNameMapping(info.Name),
                SimplePropertyMappingGenerator.AttributesTag.Evaluate(info),
                SqlUtility.Identifier(info.Name),
                MaxBlobSize);
        }

        private static IEnumerable<Type> largePropertyTypes = new[]
        {
            typeof(LongStringPropertyInfo),
            typeof(BinaryPropertyInfo)
        };

        public static bool IsSupported(PropertyInfo info)
        {
            if (!(info.DataStructure is IOrmDataStructure))
                return false;

            return largePropertyTypes.Any(supportedType => supportedType.IsAssignableFrom(info.GetType()));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PropertyInfo)conceptInfo;
            if (IsSupported(info))
                codeBuilder.InsertCode(CodeSnippet(info), OrmDataStructureMappingGenerator.MembersTag, info.DataStructure);
        }
    }
}
