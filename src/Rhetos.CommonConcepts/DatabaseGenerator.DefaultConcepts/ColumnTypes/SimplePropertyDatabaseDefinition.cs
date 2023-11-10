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
    /// A simple implementation of IConceptDatabaseGenerator for properties that use only default create/remove SQL scripts
    /// (see <see cref="PropertyDatabaseDefinition"/> class).
    /// </summary>
    /// <remarks>
    /// This helper class expects registered concept metadata for the given property concept type:
    /// <see cref="DatabaseColumnTypeBase{T}"/> and <see cref="DatabaseColumnNameBase{T}"/>
    /// The metadata is used to generate the create/remove SQL scripts.
    /// </remarks>
    public abstract class SimplePropertyDatabaseDefinition<TPropertyInfo> : IConceptDatabaseGenerator<TPropertyInfo> where TPropertyInfo : PropertyInfo
    {
        protected readonly ConceptMetadata _conceptMetadata;

        protected SimplePropertyDatabaseDefinition(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(TPropertyInfo propertyInfo, ISqlCodeBuilder sql)
        {
            if (propertyInfo.DataStructure is EntityInfo)
            {
                sql.CreateDatabaseStructure(PropertyDatabaseDefinition.AddColumn(sql.Utility, sql.Resources, _conceptMetadata, propertyInfo));
                sql.RemoveDatabaseStructure(PropertyDatabaseDefinition.RemoveColumn(sql.Utility, sql.Resources, _conceptMetadata, propertyInfo));
            }
        }
    }
}
