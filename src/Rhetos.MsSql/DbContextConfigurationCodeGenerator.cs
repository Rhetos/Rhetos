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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.EfCore;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.MsSql
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DbContextCodeGenerator))]
    public class DbContextConfigurationCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string EntityFrameworkContextSqlServerConfigurationTag = "/*EntityFrameworkContextSqlServerConfiguration*/";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(
            $@"optionsBuilder.UseSqlServer(_persistenceTransaction.Connection,
                o =>
                {{
                    o.UseRelationalNulls(_rhetosAppOptions.EntityFrameworkUseDatabaseNullSemantics);
                    {EntityFrameworkContextSqlServerConfigurationTag}
                }});
            this.Database.UseTransaction(_persistenceTransaction.Transaction);
            ",
                DbContextCodeGenerator.EntityFrameworkOnConfiguringTag);
        }
    }
}
