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
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureQueryableCodeGenerator))]
    public class OrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;
            if (info is IOrmDataStructure)
            {
                string module = info.Module.Name;
                string entity = info.Name;

                DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, $"EntityBase<{module}.{entity}>", typeof(EntityBase<>), info);

                RepositoryHelper.GenerateQueryableRepository(info, codeBuilder);
                codeBuilder.InsertCode($"Common.OrmRepositoryBase<Common.Queryable.{module}_{entity}, {module}.{entity}>", RepositoryHelper.OverrideBaseTypeTag, info);

                codeBuilder.InsertCode(
                    string.Format("public System.Data.Entity.DbSet<Common.Queryable.{0}_{1}> {0}_{1} {{ get; set; }}\r\n        ",
                        info.Module.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkContextMembersTag);
            }
        }
    }
}
