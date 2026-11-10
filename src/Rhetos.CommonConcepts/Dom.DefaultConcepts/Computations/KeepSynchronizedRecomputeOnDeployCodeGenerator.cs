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
using Rhetos.Utilities;
using System;
using System.ComponentModel.Composition;
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
                CsUtility.QuotedString(RemoveCsTags(GetRecomputeContext(info.EntityComputedFrom.Source, info.EntityComputedFrom.Target))));

            codeBuilder.InsertCode(metadata, KeepSynchronizedRecomputeOnDeployInfrastructureCodeGenerator.AddKeepSynchronizedMetadataTag);
        }

        /// <summary>
        /// This method removes tags from code snippet to prevent problems with new code accidentally injected into this tag.
        /// The text will be converted to valid C# string with <see cref="CsUtility.QuotedString"/>,
        /// but if the text contains a tag and a new code is injected here, if this code contains quotation marks,
        /// it might result with a syntax error.
        /// </summary>
        private string RemoveCsTags(string text)
        {
            return text.Replace("/*", "/*<notag>");
        }

        /// <summary>
        /// The 'Common.KeepSynchronizedMetadata.Context' property serves as cache-invalidation mechanism.
        /// If the context is changed in the new version of the application,
        /// then the old persisted data should be recomputed on deployment.
        /// This does not cover all situations when the persisted data should be recomputed
        /// on deployment, but at least handles some obvious ones.
        ///
        /// The context includes the source data structure description and the mapping of target properties
        /// computed from this source. This way, adding or removing a computed property on the target entity
        /// (without changing the source) is also detected and triggers recompute on deployment (issue #79).
        /// </summary>
        private string GetRecomputeContext(DataStructureInfo source, EntityInfo target)
        {
            var sourceContext = source is PolymorphicInfo info
                ? string.Join(", ", _dslModel.FindByReference<IsSubtypeOfInfo>(s => s.Supertype, info)
                    .Select(GetSubtypeRecomputeContext)
                    .OrderBy(description => description))
                : source.GetFullDescription();

            var targetComputedPropertiesContext = GetTargetComputedPropertiesContext(target, source);

            return sourceContext + " | TargetComputedProperties: " + targetComputedPropertiesContext;
        }

        private string GetTargetComputedPropertiesContext(EntityInfo target, DataStructureInfo source)
        {
            var computedProperties = _dslModel.FindByType<PropertyComputedFromInfo>()
                .Where(pcf => pcf.Target.DataStructure == target && pcf.Source.DataStructure == source)
                .Select(pcf => pcf.Target.Name + "=" + pcf.Source.Name)
                .OrderBy(x => x);
            return string.Join(", ", computedProperties);
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
