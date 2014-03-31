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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Autofac;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac
{
    public class UtilitiesModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<XmlUtility>().SingleInstance();

            Type sqlExecuterType = GetSqlExecuterImplementation();
            builder.RegisterType(sqlExecuterType).As<ISqlExecuter>().InstancePerLifetimeScope();

            base.Load(builder);
        }

        private static Type GetSqlExecuterImplementation()
        {
            var sqlExecuterImplementations = new Dictionary<string, Type>()
            {
                { "MsSql", typeof(MsSqlExecuter) },
                { "Oracle", typeof(OracleSqlExecuter) }
            };

            Type sqlExecuterType;
            if (!sqlExecuterImplementations.TryGetValue(SqlUtility.DatabaseLanguage, out sqlExecuterType))
                throw new FrameworkException("Unsupported database language '" + SqlUtility.DatabaseLanguage
                    + "'. Supported languages are: " + string.Join(", ", sqlExecuterImplementations.Keys) + ".");

            return sqlExecuterType;
        }
    }
}
