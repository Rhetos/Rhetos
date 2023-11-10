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

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Generates database code for the given DSL concept.
    /// </summary>
    public interface IConceptDatabaseGenerator
    {
        void GenerateCode(IConceptInfo conceptInfo, ISqlCodeBuilder sql);
    }

    /// <summary>
    /// Generates database code for the given DSL concept.
    /// </summary>
    public interface IConceptDatabaseGenerator<in TConceptInfo> : IConceptDatabaseGenerator
        where TConceptInfo : IConceptInfo
    {
        void GenerateCode(TConceptInfo conceptInfo, ISqlCodeBuilder sql);

        void IConceptDatabaseGenerator.GenerateCode(IConceptInfo conceptInfo, ISqlCodeBuilder sql)
        {
            GenerateCode((TConceptInfo)conceptInfo, sql);
        }
    }
}
