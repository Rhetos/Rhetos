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
using Rhetos.Dom.DefaultConcepts;
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
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureExtendsInfo))]
    public class DataStructureExtendsMappingGenerator : IConceptMappingCodeGenerator
    {
        public static readonly DataStructureTag MappingTagOnExtension = new DataStructureTag(TagType.Appendable, "<!-- DataStructureExtendsMappingGenerator.MappingTagOnExtension {0}.{1} -->");

        private static string BasePropertyOnExtension(DataStructureExtendsInfo info)
        {
            return string.Format(
                @"        <many-to-one name=""Base"" column=""ID"" class=""{0}, {1}"" update=""false"" insert=""false"" unique=""true"" {2} />
",
                info.Base.GetKeyProperties(),
                NHibernateMappingGenerator.AssemblyTag,
                MappingTagOnExtension.Evaluate(info.Extension));
        }

        public static readonly DataStructureTag MappingTagOnBase = new DataStructureTag(TagType.Appendable, "<!-- DataStructureExtendsMappingGenerator.MappingTagOnBase {0}.{1} -->");

        private static string ExtensionPropertyOnBase(DataStructureExtendsInfo info)
        {
            return string.Format(
                @"        <many-to-one name=""{0}"" column=""ID"" class=""{1}, {2}"" update=""false"" insert=""false"" {3} />
",
                DataStructureExtendsCodeGenerator.ExtensionPropertyName(info),
                info.Extension.GetKeyProperties(),
                NHibernateMappingGenerator.AssemblyTag,
                MappingTagOnBase.Evaluate(info.Base));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureExtendsInfo) conceptInfo;
            if (info.Extension is IOrmDataStructure && info.Base is IOrmDataStructure)
            {
                codeBuilder.InsertCode(BasePropertyOnExtension(info), OrmDataStructureMappingGenerator.MembersTag, info.Extension);
                codeBuilder.InsertCode(ExtensionPropertyOnBase(info), OrmDataStructureMappingGenerator.MembersTag, info.Base);
            }
        }
    }
}
