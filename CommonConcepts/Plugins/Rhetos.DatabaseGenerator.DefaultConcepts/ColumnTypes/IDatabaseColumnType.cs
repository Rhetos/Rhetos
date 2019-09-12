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
using System;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    public interface IDatabaseColumnType<out T> : IConceptMetadataExtension<T> where T : PropertyInfo
    {
        string ColumnType { get; }
    }

    public static class DatabaseColumnTypeHelper
    {
        public static string GetColumnType(this ConceptMetadata conceptMetadata, PropertyInfo property)
        {
            return conceptMetadata.Get<IDatabaseColumnType<PropertyInfo>>(property.GetType())?.ColumnType;
        }

        public static string GetColumnType(this ConceptMetadata conceptMetadata, Type propertyType)
        {
            return conceptMetadata.Get<IDatabaseColumnType<PropertyInfo>>(propertyType)?.ColumnType;
        }
    }
}
