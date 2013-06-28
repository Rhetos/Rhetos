/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Utilities;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    public static class Extensions
    {
        public static string GetSchemaTableForForeignKey(this DataStructureInfo dataStructure)
        {
            if (dataStructure is EntityInfo)
                return SqlUtility.Identifier(dataStructure.Module.Name)
                    + "." + SqlUtility.Identifier(dataStructure.Name);

            if (dataStructure is LegacyEntityInfo)
            {
                var legacy = (LegacyEntityInfo)dataStructure;
                return SqlUtility.GetFullName(legacy.Table);
            }

            if (dataStructure is LegacyEntityWithAutoCreatedViewInfo)
            {
                var legacy = (LegacyEntityWithAutoCreatedViewInfo)dataStructure;
                return SqlUtility.GetFullName(legacy.Table);
            }

            return null;
        }
    }
}