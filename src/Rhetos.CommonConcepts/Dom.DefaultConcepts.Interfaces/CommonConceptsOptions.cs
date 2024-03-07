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

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// Build options for Rhetos.CommonConcepts package.
    /// </summary>
    [Options("CommonConcepts")]
    public class CommonConceptsOptions
    {
        /// <summary>
        /// Option is a part of legacy design/configuration.
        /// </summary>
        public bool AutoGeneratePolymorphicProperty { get; set; } = false;

        /// <summary>
        /// Option is a part of legacy design/configuration.
        /// </summary>
        public bool CascadeDeleteInDatabase { get; set; } = false;

        public bool CompilerWarningsInGeneratedCode { get; set; } = false;

        /// <summary>
        /// Allow converting ComposableFilterBy lambda expression to simple method format,
        /// if there is no custom parameter added by UseExecutionContext concept.
        /// This will make unavailable extending custom parameters by tags AdditionalParametersTypeTag and AdditionalParametersArgumentTag.
        /// </summary>
        public bool ComposableFilterByOptimizeLambda { get; set; } = true;

        /// <summary>
        /// Allows the optimization from <see cref="ComposableFilterByOptimizeLambda"/> to be applied
        /// to filters that use repository and context arguments, by rewriting the expression to use
        /// the corresponding member variables instead of the arguments.
        /// </summary>
        public bool ComposableFilterByOptimizeRepositoryAndContextUsage { get; set; } = true;

        /// <summary>
        /// This options is only applicable to EF Core, the EF6 provider for Rhetos always uses lazy loading.
        /// If enabled on EF Core, the NuGet package "Microsoft.EntityFrameworkCore.Proxies" must be installed.
        /// </summary>
        public bool EntityFrameworkCoreUseLazyLoading { get; set; } = false;
    }
}
