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

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    /// <summary>
    /// Helper methods for configuring <see cref="TestScope"/> components in scope of a unit tests.
    /// </summary>
    public static class TestScopeContainerBuilderExtensions
    {
        public static ContainerBuilder ConfigureLogMonitor(this ContainerBuilder builder, List<string> log, EventType minLevel = EventType.Trace)
        {
            builder.RegisterInstance(new ConsoleLogProvider((eventType, eventName, message) =>
                {
                    if (eventType >= minLevel)
                        log.Add("[" + eventType + "] " + (eventName != null ? (eventName + ": ") : "") + message());
                }))
                .As<ILogProvider>();
            return builder;
        }

        public static ContainerBuilder ConfigureSqlExecuterMonitor(this ContainerBuilder builder, SqlExecuterLog sqlExecuterLog)
        {
            builder.RegisterInstance(sqlExecuterLog).ExternallyOwned();
            builder.RegisterDecorator<SqlExecuterMonitor, ISqlExecuter>();
            return builder;
        }

        public static ContainerBuilder ConfigureIgnoreClaims(this ContainerBuilder builder)
        {
            builder.RegisterType<IgnoreAuthorizationProvider>().As<IAuthorizationProvider>();
            return builder;
        }

        private class IgnoreAuthorizationProvider : IAuthorizationProvider
        {
            public IgnoreAuthorizationProvider() { }

            public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
            {
                return requiredClaims.Select(c => true).ToList();
            }
        }

        public static ContainerBuilder ConfigureFakeUser(this ContainerBuilder builder, string username)
        {
            builder.RegisterInstance(new TestUserInfo(username)).As<IUserInfo>();
            return builder;
        }

        public static ContainerBuilder ConfigureUseDatabaseNullSemantics(this ContainerBuilder builder, bool useDatabaseNullSemantics)
        {
            Console.WriteLine($"{nameof(RhetosAppOptions)}.{nameof(RhetosAppOptions.EntityFrameworkUseDatabaseNullSemantics)} = {useDatabaseNullSemantics}");
            builder.RegisterInstance(new RhetosAppOptions { EntityFrameworkUseDatabaseNullSemantics = useDatabaseNullSemantics });
            return builder;
        }
    }
}
