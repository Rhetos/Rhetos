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

namespace Rhetos.Persistence
{
    public static class EntityFrameworkMapping
    {
        /// <summary>
        /// For EDM functions inserted at this tag, set the DbFunction attribute namespace name to <see cref="ConceptualModelNamespace"/>.
        /// </summary>
        public static readonly string ConceptualModelTag = "<!--ConceptualModel-->";

        public const string ConceptualModelNamespace = "Rhetos";

        public static readonly string ConceptualModelEntityContainerTag = "<!--ConceptualModelEntityContainer-->";

        public static readonly string MappingTag = "<!--Mapping-->";

        public static readonly string MappingEntityContainerTag = "<!--MappingEntityContainer-->";

        /// <summary>
        /// For EDM functions inserted at this tag, set the DbFunction attribute namespace name to <see cref="StorageModelNamespace"/>.
        /// </summary>
        public static readonly string StorageModelTag = "<!--StorageModel-->";

        public const string StorageModelNamespace = "Rhetos.Store";

        public static readonly string StorageModelEntityContainerTag = "<!--StorageModelEntityContainer-->";

        public static readonly string ConceptualModelFileName = "ServerDomEdm.csdl";

        public static readonly string StorageModelFileName = "ServerDomEdm.ssdl";

        public static readonly string MappingModelFileName = "ServerDomEdm.msl";

        public static readonly string[] ModelFiles = new[] { ConceptualModelFileName, MappingModelFileName, StorageModelFileName };
    }
}
