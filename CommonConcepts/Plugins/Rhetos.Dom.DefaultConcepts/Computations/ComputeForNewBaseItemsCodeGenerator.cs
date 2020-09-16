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
    [ExportMetadata(MefProvider.Implements, typeof(ComputeForNewBaseItemsInfo))]
    public class ComputeForNewBaseItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComputeForNewBaseItemsInfo) conceptInfo;
            var baseDS = info.Dependency_Extends.Base;
            var uniqueSuffix = DslUtility.NameOptionalModule(info.EntityComputedFrom.Source, baseDS.Module)
                + DslUtility.NameOptionalModule(info.EntityComputedFrom.Target, baseDS.Module);

            string saveFilterArgument;
            if (!string.IsNullOrWhiteSpace(info.FilterSaveExpression))
            {
                string saveFilterMethodName = $"FilterSaveComputeForNewBaseItems_{uniqueSuffix}";
                var extensionDS = info.Dependency_Extends.Extension;

                var parsedExpression = new ParsedExpression(info.FilterSaveExpression, new[] { $"IEnumerable<{extensionDS.FullName}>" }, info);
                string filterSaveMethod = $@"private IEnumerable<{extensionDS.FullName}> {saveFilterMethodName}{parsedExpression.MethodParametersAndBody}

        ";

                codeBuilder.InsertCode(filterSaveMethod, RepositoryHelper.RepositoryMembers, baseDS);

                saveFilterArgument = $", {saveFilterMethodName}";
            }
            else
                saveFilterArgument = "";


            string callRecomputeOnSave = $@"if (insertedNew.Any())
                {{
                    Guid[] insertedIds = insertedNew.Select(item => item.ID).ToArray();
                    _domRepository.{info.EntityComputedFrom.Target.FullName}.{EntityComputedFromCodeGenerator.RecomputeFunctionName(info.EntityComputedFrom)}(insertedIds{saveFilterArgument});
                }}
                ";

            codeBuilder.InsertCode(callRecomputeOnSave, WritableOrmDataStructureCodeGenerator.OnSaveTag1, baseDS);

        }
    }
}
