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

namespace Rhetos.Utilities
{
    public class BuildOptions
    {
        public string DatabaseLanguage { get; set; } = "MsSql";
        public bool Debug { get; set; }
        public bool CommonConcepts__Legacy__AutoGeneratePolymorphicProperty { get; set; } = false;
        public bool CommonConcepts__Legacy__CascadeDeleteInDatabase { get; set; } = false;
        public InitialConceptsSort Dsl__InitialConceptsSort { get; set; } = InitialConceptsSort.Key;
        public ExcessDotInKey Dsl__ExcessDotInKey { get; set; } = ExcessDotInKey.Ignore;
        public bool Legacy__BuildResourcesFolder { get; set; } = false;
        /// <summary>
        /// Creates or updates Rhetos runtime configuration file (<see cref="RhetosAppEnvironment.ConfigurationFileName"/>)
        /// with essential information on application structure.
        /// </summary>
        public bool GenerateAppSettings { get; set; } = true;
    }

    /// <summary>
    /// Initial sorting will reduce variations in the generated application source
    /// that are created by different macro evaluation order on each deployment.
    /// Changing the sort method can be used to test if correct dependencies are specified for code generators or for database objects.
    /// </summary>
    public enum InitialConceptsSort { None, Key, KeyDescending };

    /// <summary>
    /// Before Rhetos v4.0, dot character was expected before string key parameter of current statement.
    /// Since Rhetos v4.0, dot should only be used for separating key parameters of referenced concept,
    /// but legacy syntax is allowed by setting this option to <see cref="Ignore"/> or <see cref="Warning"/>.
    /// </summary>
    public enum ExcessDotInKey { Ignore, Warning, Error };
}
