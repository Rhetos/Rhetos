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
using Rhetos.Logging;
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(KeepSynchronizedInfo))]
    public class KeepSynchronizedRecomputeOnDeployCodeGenerator : IConceptCodeGenerator
    {
        IDslModel _dslModel;

        public KeepSynchronizedRecomputeOnDeployCodeGenerator(IDslModel dslModel)
        {
            _dslModel = dslModel;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeepSynchronizedInfo)conceptInfo;

            string metadata = string.Format(
            @"new Common.KeepSynchronizedMetadata {{ Target = {0}, Source = {1}, Context = {2} }},
            ",
                CsUtility.QuotedString(info.EntityComputedFrom.Target.GetKeyProperties()),
                CsUtility.QuotedString(info.EntityComputedFrom.Source.GetKeyProperties()),
                CsUtility.QuotedString(GetRecomputeContext(info.EntityComputedFrom.Source)));

            codeBuilder.InsertCode(metadata, KeepSynchronizedRecomputeOnDeployInfrastructureCodeGenerator.AddKeepSynchronizedMetadataTag);
        }

        /// <summary>
        /// The 'Common.KeepSynchronizedMetadata.Context' property serves as cache-invalidation mechanism.
        /// If the context is changed in the new version of the application,
        /// then the old persisted data should be recomputed on deployment.
        /// This does not cover all situations when the persisted data should be recomputed
        /// on deployment, but at least handles some obvious ones.
        /// </summary>
        private string GetRecomputeContext(DataStructureInfo source)
        {
            if (source is PolymorphicInfo)
                return string.Join(", ", _dslModel.FindByReference<IsSubtypeOfInfo>(s => s.Supertype, (PolymorphicInfo)source)
                    .Select(GetSubtypeRecomputeContext)
                    .OrderBy(description => description));
            else
                return source.GetFullDescription();
        }

        private string GetSubtypeRecomputeContext(IsSubtypeOfInfo subtype)
        {
            var filters = _dslModel.FindByReference<SubtypeWhereInfo>(sw => sw.IsSubtypeOf, subtype).Select(sw => sw.Expression);
            var customSql = _dslModel.FindByReference<SpecificSubtypeSqlViewInfo>(sql => sql.IsSubtypeOf, subtype).Select(sql => sql.SqlQuery);
            var filterDescription = string.Join(" AND ", filters.Concat(customSql).OrderBy(x => x));

            string subtypeDescription = subtype.Subtype.GetKeyProperties();

            if (!string.IsNullOrEmpty(subtype.ImplementationName))
                subtypeDescription += "-" + subtype.ImplementationName;

            if (!string.IsNullOrEmpty(filterDescription))
                subtypeDescription += "(" + filterDescription + ")";

            return subtypeDescription;
        }
    }
}
