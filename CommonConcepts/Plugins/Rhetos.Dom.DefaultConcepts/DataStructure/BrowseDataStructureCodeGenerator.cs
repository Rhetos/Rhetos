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
using System.Diagnostics.Contracts;
using Rhetos.Dom;
using Rhetos.Persistence;
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(BrowseDataStructureInfo))]
    public class BrowseDataStructureCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<BrowseDataStructureInfo> BrowsePropertiesTag = "properties";

        protected static string RepositoryFunctionsSnippet(BrowseDataStructureInfo info)
        {
            return string.Format(
@"        public static IQueryable<{0}.{1}> Compute(IQueryable<{2}.{3}> source)
        {{
            return 
                from item in source
                select new {0}.{1}
                {{
                    ID = item.ID,
                    Base = item,
                    " + BrowsePropertiesTag.Evaluate(info) + @"
                }};
        }}

", info.Module.Name, info.Name, info.Source.Module.Name, info.Source.Name);
        }

        protected static string QuerySnippet(BrowseDataStructureInfo info)
        {
            return string.Format(
                @"return Compute(_domRepository.{0}.{1}.Query());",
                info.Source.Module.Name, info.Source.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (BrowseDataStructureInfo)conceptInfo;

            PropertyInfo idProperty = new PropertyInfo { DataStructure = info, Name = "ID" };
            PropertyHelper.GenerateCodeForType(idProperty, codeBuilder, "Guid", true);
            codeBuilder.AddInterfaceAndReference(typeof(IEntity), info);

            RepositoryHelper.GenerateRepository(info, codeBuilder);
            RepositoryHelper.GenerateQueryableRepositoryFunctions(info, codeBuilder, QuerySnippet(info));
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
        }
    }
}
