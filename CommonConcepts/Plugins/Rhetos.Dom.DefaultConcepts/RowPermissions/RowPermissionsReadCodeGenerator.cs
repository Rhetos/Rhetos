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
    [ExportMetadata(MefProvider.Implements, typeof(RowPermissionsReadInfo))]
    public class RowPermissionsReadCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (RowPermissionsReadInfo)conceptInfo;
            var queryableType = $"Common.Queryable.{info.Source.Module.Name}_{info.Source.Name}";
            var methodArguments = new[]
            {
                $"IQueryable<{queryableType}>",
                "Common.DomRepository",
                "Common.ExecutionContext"
            };
            var methodResult = $"Expression<Func<{queryableType}, bool>>";

            var parsedExpression = new ParsedExpression(info.SimplifiedExpression, methodArguments, info);
            var method = $@"public {methodResult} {RowPermissionsReadInfo.PermissionsExpressionName}{parsedExpression.MethodParametersAndBody}

        ";
            codeBuilder.InsertCode(method, RepositoryHelper.RepositoryMembers, info.Source);
        }
    }
}
