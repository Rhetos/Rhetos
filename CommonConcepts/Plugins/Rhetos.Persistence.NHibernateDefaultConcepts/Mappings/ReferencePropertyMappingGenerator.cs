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

namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(IConceptMappingCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyMappingGenerator : IConceptMappingCodeGenerator
    {
		// NH does not allow both Guid and navigation properties to be mapped to the same table column, unless one of the properties is not writable (insert and update mapping set to "false").
		// If the Guid property was defined *after* the navigation property in the .xml mapping file, the lazy loading of the reference would not work.
        private string SnippetMapping(ReferencePropertyInfo info)
        {
            return string.Format(
@"        <property {4} update=""false"" insert=""false"" />
        <many-to-one {2} class=""{0}.{1}, " + NHibernateMappingGenerator.AssemblyTag + @""" {3} />
",
                info.Referenced.Module.Name,
                info.Referenced.Name,
                NhUtility.PropertyAndColumnNameMapping(info.Name, info.Name + "ID"),
                string.Format(MappingTag, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name),
                NhUtility.PropertyAndColumnNameMapping(info.Name + "ID"));
        }

        public const string MappingTag = "<!-- reference {0}.{1}.{2} -->";
        
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReferencePropertyInfo)conceptInfo;
            if (info.DataStructure is IOrmDataStructure)
                codeBuilder.InsertCode(SnippetMapping(info), OrmDataStructureMappingGenerator.MembersTag, info.DataStructure);
        }
    }
}
