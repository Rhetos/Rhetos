﻿/*
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
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator
{
    public interface IConceptDataMigration
    {
        void GenerateCode(IConceptInfo concept, IDataMigrationScriptBuilder codeBuilder);
    }

    public interface IConceptDataMigration<in TConceptInfo> : IConceptDataMigration where TConceptInfo : IConceptInfo
    {
        void GenerateCode(TConceptInfo concept, IDataMigrationScriptBuilder codeBuilder);

        void IConceptDataMigration.GenerateCode(IConceptInfo conceptInfo, IDataMigrationScriptBuilder codeBuilder)
        {
            GenerateCode((TConceptInfo)conceptInfo, codeBuilder);
        }
    }
}
