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

namespace Rhetos.Utilities
{
    [Options("Rhetos:Build")]
    public class BuildOptions
    {
        public InitialConceptsSort InitialConceptsSort { get; set; } = InitialConceptsSort.Key;
        
        public ExcessDotInKey DslSyntaxExcessDotInKey { get; set; } = ExcessDotInKey.Ignore;

        /// <summary>
        /// Copies legacy assets files from 'Resources' folders, from all referenced packages, into the 'RhetosAssets' folder of the generated application.
        /// It also included files from the current project's folder 'Resources\Rhetos', to avoid conflicts with other usages of the project's Resources folder.
        /// Each file is copied into the subfolder with its source package name.
        /// </summary>
        /// <remarks>
        /// This is a legacy feature to support old Rhetos plugins.
        /// </remarks>
        public bool BuildResourcesFolder { get; set; } = false;

        /// <summary>
        /// Specifies maximum number of parallel threads while executing generators. 0 = unlimited.
        /// </summary>
        public int MaxExecuteGeneratorsParallelism { get; set; } = 0;

        /// <summary>
        /// List of pairs specifying additional dependencies for generators. Each entry is formatted as "GeneratorTypeFullName:GeneratorDependencyTypeFullName".
        /// </summary>
        public IEnumerable<string> AdditionalGeneratorDependencies { get; set; }
    }

    /// <summary>
    /// Initial sorting will reduce variations in the generated application source
    /// that are created by different macro evaluation order on each deployment.
    /// Changing the sort method can be used to test if correct dependencies are specified for code generators or for database objects.
    /// </summary>
    public enum InitialConceptsSort { None, Key, KeyDescending };
}
