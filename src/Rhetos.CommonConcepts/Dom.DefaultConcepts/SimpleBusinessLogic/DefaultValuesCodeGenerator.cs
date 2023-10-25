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
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DefaultValuesInfo))]
    public class DefaultValuesCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DefaultValuesInfo> BeforeDefaultValuesTag = "BeforeDefaultValues";
        public static readonly CsTag<DefaultValuesInfo> SetDefaultValueTag = "SetDefaultValue";
        public static readonly CsTag<DefaultValuesInfo> AfterDefaultValuesTag = "AfterDefaultValues";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DefaultValuesInfo)conceptInfo;

            string saveCode =
            $@"InitializeDefaultValues(insertedNew);

            ";

            codeBuilder.InsertCode(saveCode, WritableOrmDataStructureCodeGenerator.InitializationTag, info.DataStructure);

            string initializeDefaultValuesCode =
        $@"public void InitializeDefaultValues(IEnumerable<global::{info.DataStructure.Module.Name}.{info.DataStructure.Name}> items)
        {{
            {BeforeDefaultValuesTag.Evaluate(info)}
            foreach (var item in items)
            {{
                {SetDefaultValueTag.Evaluate(info)}
            }}
            {AfterDefaultValuesTag.Evaluate(info)}
        }}

        ";

            codeBuilder.InsertCode(initializeDefaultValuesCode, RepositoryHelper.RepositoryMembers, info.DataStructure);
        }
    }
}
