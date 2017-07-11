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
using Rhetos.Utilities;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Processing;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryableExtensionInfo))]
    public class QueryableExtensionCodeGenerator : IConceptCodeGenerator
    {
        protected static string RepositoryFunctionsSnippet(QueryableExtensionInfo info)
        {
            return string.Format(
        @"public readonly Func<IQueryable<Common.Queryable.{2}_{3}>, Common.DomRepository{4}, IQueryable<Common.Queryable.{0}_{1}>> Compute =
            {5};

        ",
                info.Module.Name, info.Name, info.Base.Module.Name, info.Base.Name, DataStructureUtility.ComputationAdditionalParametersTypeTag.Evaluate(info), info.Expression);
        }

        protected static string QuerySnippet(QueryableExtensionInfo info)
        {
            return string.Format(
                @"return Compute(_domRepository.{0}.{1}.Query(), _domRepository{2});",
                info.Base.Module.Name, info.Base.Name, DataStructureUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryableExtensionInfo)conceptInfo;

            DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, $"EntityBase<{info.Module.Name}.{info.Name}>", typeof(EntityBase<>), info);

            RepositoryHelper.GenerateQueryableRepository(info, codeBuilder, QuerySnippet(info));
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
        }
    }
}
