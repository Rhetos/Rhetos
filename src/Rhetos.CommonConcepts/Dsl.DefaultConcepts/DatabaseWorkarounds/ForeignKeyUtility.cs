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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.Dsl.DefaultConcepts
{
    public static class ForeignKeyUtility
    {
        /// <summary>
        /// Note: When using this function to create a database object, always add the dependencies from GetAdditionalForeignKeyDependencies().
        /// </summary>
        public static string GetSchemaTableForForeignKey(DataStructureInfo dataStructure, ISqlUtility sqlUtility)
        {
            if (dataStructure is EntityInfo)
                return sqlUtility.Identifier(dataStructure.Module.Name)
                    + "." + sqlUtility.Identifier(dataStructure.Name);

            if (dataStructure is LegacyEntityInfo)
            {
                var legacy = (LegacyEntityInfo)dataStructure;
                return sqlUtility.GetFullName(legacy.Table);
            }

            if (dataStructure is LegacyEntityWithAutoCreatedViewInfo)
            {
                var legacy = (LegacyEntityWithAutoCreatedViewInfo)dataStructure;
                return sqlUtility.GetFullName(legacy.Table);
            }

            if (dataStructure is PolymorphicInfo)
                return dataStructure.FullName + "_Materialized";

            return null;
        }

        public static IEnumerable<IConceptInfo> GetAdditionalForeignKeyDependencies(DataStructureInfo dataStructure)
        {
            if (dataStructure is PolymorphicInfo polymorphicInfo)
                return new IConceptInfo[] { polymorphicInfo.GetMaterializedEntity() };

            return Array.Empty<IConceptInfo>();
        }
    }
}