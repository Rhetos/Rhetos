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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(KeepSynchronizedInfo))]
    public class KeepSynchronizedRecomputeOnDeployCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (KeepSynchronizedInfo)conceptInfo;

            codeBuilder.InsertCode(KeepSynchronizedMetadataSnippet(info), KeepSynchronizedRecomputeOnDeployInfrastructureCodeGenerator.AddKeepSynchronizedMetadataTag);
        }

        private static string KeepSynchronizedMetadataSnippet(KeepSynchronizedInfo info)
        {
            return string.Format(
            @"new Common.KeepSynchronizedMetadata {{ Target = {0}, Source = {1}, Context = {2} }},
            ",
                CsUtility.QuotedString(info.EntityComputedFrom.Target.GetKeyProperties()),
                CsUtility.QuotedString(info.EntityComputedFrom.Source.GetKeyProperties()),
                CsUtility.QuotedString(GetKeepSynchronizedContext(info)));
        }

        /// <summary>
        /// The Context property serves as cache-invalidation mechanism.
        /// When the context is changed in the new version of the application,
        /// then the old persisted data should be recomputed on deployment.
        /// This does not cover all situations when the persisted data should be recomputed
        /// on deployment, but at least handles some obvious ones.
        /// </summary>
        private static string GetKeepSynchronizedContext(KeepSynchronizedInfo info)
        {
            return info.EntityComputedFrom.Source.GetFullDescription();
        }
    }
}
