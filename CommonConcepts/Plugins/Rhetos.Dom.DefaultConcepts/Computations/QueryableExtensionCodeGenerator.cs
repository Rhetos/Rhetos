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
    [ExportMetadata(MefProvider.Implements, typeof(QueryableExtensionInfo))]
    public class QueryableExtensionCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryableExtensionInfo)conceptInfo;

            string parameterTypesTag = DataStructureUtility.ComputationAdditionalParametersTypeTag.Evaluate(info);
            string parameterArgsTag = DataStructureUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info);
            string baseIQueryable = $"IQueryable<Common.Queryable.{info.Base.Module.Name}_{info.Base.Name}>";
            string extensionIQueryable = $"IQueryable<Common.Queryable.{info.Module.Name}_{info.Name}>";

            string querySnippet =
            $@"Func<{baseIQueryable}, Common.DomRepository{parameterTypesTag}, {extensionIQueryable}> Compute =
                {info.Expression};
            return Compute(_domRepository.{info.Base.FullName}.Query(), _domRepository{parameterArgsTag});";

            DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, $"EntityBase<{info.FullName}>", typeof(EntityBase<>), info);
            RepositoryHelper.GenerateQueryableRepository(info, codeBuilder, querySnippet);
        }
    }
}
