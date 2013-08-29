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
using Rhetos.Compiler;
using Rhetos.Persistence;
using Rhetos.Persistence.NHibernate;
using Rhetos.Utilities;

namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(IConceptMappingCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class OrmDataStructureMappingGenerator : IConceptMappingCodeGenerator
    {
        public static readonly XmlTag<DataStructureInfo> MembersTag = "Orm Members";

        private string GenerateMapping(DataStructureInfo info)
        {
            return
@"
    <class name=""{0}.{1}, " + NHibernateMappingGenerator.AssemblyTag + @""" schema=""{2}"" table=""{3}"">
        <id name=""ID"" />
" + MembersTag.Evaluate(info) + @"
    </class>
";
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            if (conceptInfo is IOrmDataStructure)
            {
                var info = (DataStructureInfo)conceptInfo;
                var orm = (IOrmDataStructure)conceptInfo;
                codeBuilder.InsertCode(
                    string.Format(CultureInfo.InvariantCulture, GenerateMapping(info),
                        info.Module.Name,
                        info.Name,
                        SqlUtility.Identifier(orm.GetOrmSchema()),
                        SqlUtility.Identifier(orm.GetOrmDatabaseObject())),
                    string.Format(CultureInfo.InvariantCulture, ModuleMappingGenerator.ModuleTag, info.Module.Name));
            }
        }
    }
}
