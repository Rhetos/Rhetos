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
using Autofac.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MultitenantDatabasesTest
    {
        /// <summary>
        /// This test checks if any singleton components depend on a <see cref="ConnectionString"/> or any "matching scope lifetime" component such as <see cref="Rhetos.Persistence.IPersistenceTransaction"/>.
        /// That would mean that the database connection is captured in the singleton component during the whole runtime of the application. That case has multiple issues,
        /// but if Rhetos is used to build a multitenant application that has single app instance and multiple databases, then the global <see cref="ConnectionString"/> would cause
        /// issues because it might not be related to any tenant, or the singleton component might capture any first tenant and use it for all other requests, even for other tenants.
        /// Therefore, core singleton components should not depend on <see cref="ConnectionString"/>, even though the <see cref="ConnectionString"/> is singleton by default, but it might not be in some multitenant apps.
        /// </summary>
        [TestMethod]
        public void InvalidScopeDependenciesWithConnectionString()
        {
            using var scope = TestScope.Create();

            var lifetimeScope = (ILifetimeScope)scope.GetType().GetField("_lifetimeScope", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scope);
            var registrations = GetRegistrations(lifetimeScope);

            var componentsDependsOnService = registrations
                .SelectMany(r => r.Component.GetConstructors()
                    .SelectMany(c => c.GetParameters()
                        .Select(cp => new ComponentDependency(r.Component, cp.ParameterType, cp.Name, cp.ParameterType.ToString()))))
                .OrderBy(c => c.Component.ToString())
                .ToList();

            var registrationsByComponent = registrations.GroupBy(r => r.Component).ToDictionary(g => g.Key, g => g.ToList());

            var matchingScopeComponents = registrations.Where(r => r.Lifetime is RegistrationLifetime.MatchingScope)
                .Select(r => r.Component)
                // In some multitenant applications, the connection string can be registered with "matching scope" lifetime,
                // so it is important to make sure that no component out of that lifetime (for example, a singleton) depends on the connection string.
                .Concat([typeof(ConnectionString)])
                .ToList();

            HashSet<string> allowedExceptions =
            [
                // Ef6InitializationConnectionString is a singleton that captures ConnectionString, but in a multitenant app where the ConnectionString is scoped,
                // the Ef6InitializationConnectionString will have custom registration in a way not to use the scoped tenant-specific ConnectionString.
                "Rhetos.MsSqlEf6.CommonConcepts.Ef6InitializationConnectionString",

                // PluginsMetadataCache is a singleton that can resolve scoped components, but it is implemented in a way to never keep the resolved instances.
                "Rhetos.Extensibility.PluginsMetadataCache`1",
            ];

            var dependentRegistrations = GetDependentComponents(matchingScopeComponents, registrations, componentsDependsOnService, allowedExceptions)
                .Select(d => new { d.Component, d.Report, Registrations = registrationsByComponent[d.Component] })
                .ToList();

            var invalidDependents = dependentRegistrations.Where(d => d.Registrations.Any(r => r.Lifetime is RegistrationLifetime.Root)).ToList();

            string reportInvalidDependents = string.Join(Environment.NewLine, invalidDependents.Select((d, x) => $"{x+1}. {d.Component} {d.Report}"));
            Assert.AreEqual("", reportInvalidDependents);
        }

        private static List<(Type Component, string Report)> GetDependentComponents(List<Type> dependencies, List<RegistrationInfo> registrations, List<ComponentDependency> componentsDependsOnService, HashSet<string> allowedExceptions)
        {
            List<(Type Component, string Report)> dependentComponents = new();
            List<(Type Component, string Report)> newDependentComponents = dependencies.Select(t => (Component: t, Report: t.Name)).ToList();

            while (newDependentComponents.Any())
            {
                var dependentServices = newDependentComponents
                    .SelectMany(dr => registrations
                        .Where(r => r.Component == dr.Component)
                        .SelectMany(r => r.Services.Select(s => new { Service = s, dr.Report })))
                    .Concat(newDependentComponents.Select(c => new { Service = c.Component.ToString(), Report = c.Report }))
                    .Distinct()
                    .ToMultiDictionary(ds => ds.Service, ds => ds.Report);

                newDependentComponents = dependentServices.SelectMany(ds => componentsDependsOnService.Where(c => c.DependsOnService.Contains(ds.Key))
                    .Select(c => new { c.Component, c.ConstructorParameterType, c.ConstructorParameterName, DependsOn = ds.Key, DependsOnReport = ds.Value.First() }))
                    .Where(c => !dependentComponents.Any(dr => dr.Component == c.Component))
                    .Select(c => (c.Component, Report: $"{c.Component.Name}({c.ConstructorParameterType.Name} {c.ConstructorParameterName}) => {c.DependsOnReport}"))
                    .ToList();

                newDependentComponents.RemoveAll(d => allowedExceptions.Contains(GetFullNameWithoutGenericParameters(d.Component)));

                dependentComponents.AddRange(newDependentComponents);
            }

            return dependentComponents;
        }

        private static string GetFullNameWithoutGenericParameters(Type t) => $"{t.Namespace}.{t.Name}";

        enum RegistrationLifetime { Root, Scope, MatchingScope };

        record RegistrationInfo(int Level, Type Component, List<string> Services, IComponentRegistration Registration, RegistrationLifetime Lifetime);

        record ComponentDependency(Type Component, Type ConstructorParameterType, string ConstructorParameterName, string DependsOnService);

        List<RegistrationInfo> GetRegistrations(ILifetimeScope ls)
        {
            List<RegistrationInfo> result = new();
            int level = 0;
            IEnumerable<IComponentRegistration> lastRegistrations = null;
            while (ls != null)
            {
                if (lastRegistrations != ls.ComponentRegistry.Registrations)
                    result.AddRange(ls.ComponentRegistry.Registrations.Select(r =>
                        new RegistrationInfo(
                            level,
                            r.Activator.LimitType,
                            GetListOrDefault(r.Services.Select(s => s.Description).Where(sd => sd != "AutoActivate").ToList(), r.Activator.LimitType.ToString()),
                            r,
                            r.Lifetime switch
                            {
                                Autofac.Core.Lifetime.MatchingScopeLifetime => RegistrationLifetime.MatchingScope,
                                Autofac.Core.Lifetime.CurrentScopeLifetime => RegistrationLifetime.Scope,
                                Autofac.Core.Lifetime.RootScopeLifetime => RegistrationLifetime.Root,
                                _ => throw new ArgumentException()
                            }
                        )));
                lastRegistrations = ls.ComponentRegistry.Registrations;
                ls = (ls as ISharingLifetimeScope)?.ParentLifetimeScope;
                level++;
            }
            return result.OrderBy(r => r.Component.ToString()).ToList();
        }

        List<string> GetListOrDefault(List<string> list, string defaultElement)
        {
            list = list.Any() ? list : [defaultElement];
            return list.Select(ClearKeyedRegistration).ToList();
        }

        string ClearKeyedRegistration(string s)
        {
            int start = s.IndexOf('(');
            if (start != -1 && s.Last() == ')')
                return s.Substring(start + 1, s.Length - start - 2);
            else
                return s;
        }
    }
}
