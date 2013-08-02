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
    [ExportMetadata(MefProvider.Implements, typeof(LinkedItemsInfo))]
    public class LinkedItemsMappingGenerator : IConceptMappingCodeGenerator
    {
        private const string CodeSnippet =
@"        <bag name=""{2}"" inverse=""true"" cascade=""all"">
            <key column=""{3}"" />
            <one-to-many class=""{0}.{1}, " + NHibernateMappingGenerator.AssemblyTag + @""" />
        </bag>
";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LinkedItemsInfo)conceptInfo;
            codeBuilder.InsertCode(
                string.Format(CultureInfo.InvariantCulture,
                    CodeSnippet,
                        info.ReferenceProperty.DataStructure.Module.Name,
                        info.ReferenceProperty.DataStructure.Name,
                        info.Name,
                        SqlUtility.Identifier(info.ReferenceProperty.Name + "ID")),
                OrmDataStructureMappingGenerator.MembersTag, info.DataStructure);
        }
    }
}
