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
using System;

namespace Rhetos.DatabaseGenerator
{
    [Obsolete("Use IConceptDatabaseGenerator interface instead of IConceptDatabaseDefinition.")]
    public interface IConceptDatabaseDefinition : IConceptDatabaseGenerator
    {
        string CreateDatabaseStructure(IConceptInfo conceptInfo);

        string RemoveDatabaseStructure(IConceptInfo conceptInfo);

#pragma warning disable CA1033 // Interface methods should be callable by child types
        void IConceptDatabaseGenerator.GenerateCode(IConceptInfo conceptInfo, ISqlCodeBuilder sql)
        {
            GenerateCodeIConceptDatabaseDefinition(conceptInfo, sql);
        }
#pragma warning restore CA1033 // Interface methods should be callable by child types

        protected void GenerateCodeIConceptDatabaseDefinition(IConceptInfo conceptInfo, ISqlCodeBuilder sql)
        {
            sql.CreateDatabaseStructure(CreateDatabaseStructure(conceptInfo));
            sql.RemoveDatabaseStructure(RemoveDatabaseStructure(conceptInfo));
        }
    }
}
