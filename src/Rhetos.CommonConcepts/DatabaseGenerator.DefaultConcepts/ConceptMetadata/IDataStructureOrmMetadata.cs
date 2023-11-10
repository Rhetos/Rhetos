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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    /// <summary>
    /// Interface for <see cref="ConceptMetadata"/> plugins that provides ORM database mapping for data structures.
    /// </summary>
    public interface IDataStructureOrmMetadata : IConceptMetadataExtension
    {
        /// <summary>
        /// Returns schema name of the corresponding database object. It will be mapped to the C# class when loading or saving the data.
        /// </summary>
        string GetOrmSchemaGeneric(DataStructureInfo dataStructure);

        /// <summary>
        /// Returns the corresponding database object name. It will be mapped to the C# class when loading or saving the data.
        /// </summary>
        string GetOrmDatabaseObjectGeneric(DataStructureInfo dataStructure);
    }

    /// <summary>
    /// Helper for implementation of <see cref="ConceptMetadata"/> plugins that provides ORM database mapping for data structures.
    /// </summary>
    public abstract class DataStructureOrmMetadataBase<T> : IConceptMetadataExtension<T>, IDataStructureOrmMetadata
        where T : DataStructureInfo
    {
        public string GetOrmSchemaGeneric(DataStructureInfo dataStructure) => GetOrmSchema((T)dataStructure);

        public string GetOrmDatabaseObjectGeneric(DataStructureInfo dataStructure) => GetOrmDatabaseObject((T)dataStructure);

        public abstract string GetOrmSchema(T dataStructure);

        public abstract string GetOrmDatabaseObject(T dataStructure);
    }
}

namespace Rhetos.Dsl
{
    using Rhetos.DatabaseGenerator.DefaultConcepts;

    public static class DataStructureOrmHelper
    {
        /// <summary>
        /// Returns schema name of the corresponding database object. It will be mapped to the C# class when loading or saving the data.
        /// </summary>
        public static string GetOrmSchema(this ConceptMetadata conceptMetadata, DataStructureInfo dataStructure)
        {
            return conceptMetadata.Get<IDataStructureOrmMetadata>(dataStructure.GetType())?.GetOrmSchemaGeneric(dataStructure);
        }

        /// <summary>
        /// Returns the corresponding database object name. It will be mapped to the C# class when loading or saving the data.
        /// </summary>
        public static string GetOrmDatabaseObject(this ConceptMetadata conceptMetadata, DataStructureInfo dataStructure)
        {
            return conceptMetadata.Get<IDataStructureOrmMetadata>(dataStructure.GetType())?.GetOrmDatabaseObjectGeneric(dataStructure);
        }
    }
}
