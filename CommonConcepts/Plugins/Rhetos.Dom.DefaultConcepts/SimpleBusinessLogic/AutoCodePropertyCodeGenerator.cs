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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodePropertyInfo))]
    public class AutoCodePropertyCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<AutoCodePropertyInfo> GroupingTag = "Grouping";
        public static readonly CsTag<AutoCodePropertyInfo> GroupColumnMetadataTag = "GroupColumnMetadata";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (AutoCodePropertyInfo)conceptInfo;

            string dataStructure = info.Property.DataStructure.Module.Name + "." + info.Property.DataStructure.Name;
            string snippet =
            $@"AutoCodeHelper.UpdateCodesWithoutCache(
                _executionContext.SqlExecuter, ""{dataStructure}"", ""{info.Property.Name}"",
                insertedNew.Select(item => AutoCodeItem.Create(item, item.{info.Property.Name}{GroupingTag.Evaluate(info)})).ToList(),
                (item, newCode) => item.{info.Property.Name} = newCode{GroupColumnMetadataTag.Evaluate(info)});

            ";

            // Inserting snippet at OldDataLoadedTag instead of InitializationTag, to ensure that
            // default values for code format or grouping can be set before generating the autocode.
            codeBuilder.InsertCode(snippet, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Property.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(AutoCodeHelper));
            codeBuilder.AddReferencesFromDependency(typeof(AutoCodeItem));
        }
    }
}
