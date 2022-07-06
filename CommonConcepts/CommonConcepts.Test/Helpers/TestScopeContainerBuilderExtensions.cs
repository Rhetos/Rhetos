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

using Autofac;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// This method uses a shallow copy of the original options instance for configuration.
        /// It supports <paramref name="configure"/> action that directly modifies properties of the options class.
        /// </summary>
        /// <remarks>
        /// Since options classes are usually singletons, the action must not modify an object that is referenced
        /// by the options class, without modifying the options class property,
        /// because it might affect configuration of other unit tests.
        /// </remarks>
        public static ContainerBuilder ConfigureOptions<TOptions>(this ContainerBuilder builder, Action<TOptions> configure) where TOptions : class
        {
            TOptions copy;
            using (var scope = TestScope.Create())
            {
                var options = scope.Resolve<TOptions>();
                // Options classes as usually singleton, so we are making a copy to avoid affecting configuration of other tests.
                copy = CsUtility.ShallowCopy(options);
            }

            configure.Invoke(copy);
            builder.RegisterInstance(copy);
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

        /// <summary>
        /// Set <paramref name="username"/> to <see langword="null"/> for anonymous user.
        /// </summary>
        public static ContainerBuilder ConfigureFakeUser(this ContainerBuilder builder, string username)
        {
            if (username != null)
                builder.RegisterInstance(new TestUserInfo(username)).As<IUserInfo>();
            else
                builder.RegisterInstance(new TestUserInfo(null, "", isUserRecognized: false)).As<IUserInfo>();
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
