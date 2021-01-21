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
using CommonConcepts.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rhetos
{
    [TestClass]
    public class RhetosRuntimeTest
    {
        [TestMethod]
        public void RuntimeRegistrationsRegressionTest()
        {
            using (var scope = TestScope.Create())
            {
                var lifetimeScope = (Lazy<ILifetimeScope>)scope.GetType()
                    .GetField("_lifetimeScope", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(scope);

                var registrations = lifetimeScope.Value.ComponentRegistry.Registrations
                    .Select(registration => registration.ToString())
                    .OrderBy(text => text)
                    // Removing repository class registrations. This test is focused on system components, not business features in test DSL scripts.
                    .Where(text => !text.Contains("_Repository ("))
                    // Removing implementations of generic interfaces. They are added dynamically at runtime as an internal Autofac caching mechanism.
                    .Where(text => !_genericInterfaceRegistration.IsMatch(text))
                    .ToList();

                foreach (string line in registrations)
                    Console.WriteLine(line);

                TestUtility.AssertAreEqualByLine(_expectedRegistrationsRuntime.Trim(), string.Join("\r\n", registrations));
            }
        }

        private static readonly Regex _genericInterfaceRegistration = new Regex(@"`\d+\[\["); // For example IEnumerable`1[[SomeType]]

        const string _expectedRegistrationsRuntime =
@"
Activator = ApplyFiltersOnClientRead (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.ApplyFiltersOnClientRead], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = AppSecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.AppSecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationDataCache (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.AuthorizationDataCache, Rhetos.Dom.DefaultConcepts.IAuthorizationData], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationDataLoader (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.AuthorizationDataLoader], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = BinaryCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = BinaryDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = BoolCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = BoolDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ClaimsNotRelatedToServerCommands (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.DummyCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CommonAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CommonConceptsDatabaseSettings (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsDatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CommonConceptsOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CommonConceptsOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CommonConceptsRuntimeOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsRuntimeOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConsoleLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CurrentKeepSynchronizedMetadata (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.CurrentKeepSynchronizedMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DateCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DateDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DateTimeCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DateTimeDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DecimalCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DecimalDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomRepository (ReflectionActivator), Services = [Common.DomRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DownloadReportCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.DownloadReportCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DownloadReportCommandClaims (ReflectionActivator), Services = [Rhetos.Processing.DefaultCommands.DownloadReportCommandClaims], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DownloadReportCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.DownloadReportCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DownloadReportCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DummyCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewCacheFactory (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewCacheFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewsFileStore (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsFileStore], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewsHash (ReflectionActivator), Services = [Rhetos.Persistence.IEfMappingViewsHash], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkConfiguration (ReflectionActivator), Services = [System.Data.Entity.DbConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkContext (ReflectionActivator), Services = [Common.EntityFrameworkContext, System.Data.Entity.DbContext, Rhetos.Persistence.IPersistenceCache], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkMetadata (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.Persistence.EntityFrameworkMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ExecuteActionCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.ExecuteActionCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ExecuteActionCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.ExecuteActionCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ExecuteActionCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ExecutionContext (ReflectionActivator), Services = [Common.ExecutionContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GenericFilterHelper (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericFilterHelper], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = GenericRepositories (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericRepositories], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = GenericRepositoryParameters (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericRepositoryParameters], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = GuidCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = GuidDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages, Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = IntegerCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = IntegerDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = LoadDslModelCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.LoadDslModelCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LoadDslModelCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.LoadDslModelCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LoadDslModelCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LongStringCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LongStringDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MetadataWorkspaceFileProvider (ReflectionActivator), Services = [Rhetos.Persistence.IMetadataWorkspaceFileProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MoneyCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MoneyDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceStorage (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorage], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceStorageObjectMappings (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorageObjectMappings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = PingCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.PingCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PingCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PrincipalWriter (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.PrincipalWriter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PropertyDatabaseColumnNameMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = QueryDataSourceCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.QueryDataSourceCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = QueryDataSourceCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.QueryDataSourceCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = QueryDataSourceCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReadCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.ReadCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReadCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.ReadCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReadCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReferenceCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReferenceDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ReferencePropertyDatabaseColumnNameMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RegisteredInterfaceImplementations (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.RegisteredInterfaceImplementations], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = RhetosAppEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SaveCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.SaveEntityCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SaveEntityCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.SaveEntityCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SaveEntityCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ServerCommandsUtility (ReflectionActivator), Services = [Rhetos.Processing.DefaultCommands.ServerCommandsUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ShortStringCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ShortStringDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SqlCommandBatch (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorageCommandBatch], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SqlObjectsIndex (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
";
    }
}