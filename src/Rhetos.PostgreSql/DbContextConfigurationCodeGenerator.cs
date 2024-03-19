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

namespace Rhetos.PostgreSql
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DbContextCodeGenerator))]
    public class DbContextConfigurationCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string EntityFrameworkContextPostgreSqlOptionsTag = "/*EntityFrameworkContextPostgreSqlOptions*/";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(
                $@"optionsBuilder.UseNpgsql(context.Resolve<Rhetos.Persistence.IPersistenceTransaction>().Connection, sqlOptions =>
                {{
                    sqlOptions.UseRelationalNulls(context.Resolve<RhetosAppOptions>().EntityFrameworkUseDatabaseNullSemantics);
                    {EntityFrameworkContextPostgreSqlOptionsTag}
                    configurePostreSqlOptions?.Invoke(sqlOptions);
                }});
                ",
                DbContextCodeGenerator.EntityFrameworkContextOptionsBuilderTag);

            codeBuilder.InsertCode(",\r\n            Action<Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder> configurePostreSqlOptions = null", DbContextCodeGenerator.EntityFrameworkContextOptionsParameterDeclarationTag);
            codeBuilder.InsertCode(", configurePostreSqlOptions", DbContextCodeGenerator.EntityFrameworkContextOptionsParameterCallTag);
        }
    }
}
