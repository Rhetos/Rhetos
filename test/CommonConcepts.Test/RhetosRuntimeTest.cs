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
using Autofac.Core.Registration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RhetosRuntimeTest
    {
        [TestMethod]
        public void RuntimeRegistrationsRegressionTest()
        {
            var lifetimeScope = (ILifetimeScope)TestScope.RhetosHost.Value.GetRootContainer();

            var registrations = lifetimeScope.ComponentRegistry.Registrations
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

        private static readonly Regex _genericInterfaceRegistration = new Regex(@"`\d+\[\["); // For example IEnumerable`1[[SomeType]]

        const string _expectedRegistrationsRuntime =
@"
Activator = ApplyFiltersOnClientRead (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.ApplyFiltersOnClientRead], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = AppSecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.AppSecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = AuthorizationDataCache (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.AuthorizationDataCache, Rhetos.Dom.DefaultConcepts.IAuthorizationData], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = AuthorizationDataLoader (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.AuthorizationDataLoader], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = BinaryCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = BinaryDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = BoolCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = BoolDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ClaimsNotRelatedToServerCommands (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.DummyCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CommonAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CommonConceptsDatabaseSettings (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsDatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CommonConceptsOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CommonConceptsOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CommonConceptsRuntimeOptions (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.CommonConceptsRuntimeOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConnectionString (ReflectionActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConsoleLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CurrentKeepSynchronizedMetadata (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.CurrentKeepSynchronizedMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseOptions (DelegateActivator), Services = [Rhetos.Utilities.DatabaseOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DateCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DateDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DateTimeCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DateTimeDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DecimalCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DecimalDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DomRepository (ReflectionActivator), Services = [Common.DomRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DownloadReportCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.DownloadReportCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DownloadReportCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.DownloadReportCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DownloadReportCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DummyCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = Ef6OrmUtility (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IOrmUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewCacheFactory (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewCacheFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewsFileStore (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsFileStore], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewsHash (ReflectionActivator), Services = [Rhetos.Persistence.IEfMappingViewsHash], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewsInitializer (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsInitializer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EntityFrameworkConfiguration (ReflectionActivator), Services = [System.Data.Entity.DbConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EntityFrameworkContext (ReflectionActivator), Services = [Common.EntityFrameworkContext, System.Data.Entity.DbContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EntityFrameworkMetadata (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.Persistence.EntityFrameworkMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EntityOrmMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ExecuteActionCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.ExecuteActionCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ExecuteActionCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.ExecuteActionCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ExecuteActionCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ExecutionContext (ReflectionActivator), Services = [Common.ExecutionContext], Lifetime = Autofac.Core.Lifetime.MatchingScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = GenericFilterHelper (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericFilterHelper], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = GenericRepositories (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericRepositories], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = GenericRepositoryParameters (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.GenericRepositoryParameters], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = GuidCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = GuidDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = IDataStructureReadParameters (DelegateActivator), Services = [Rhetos.Dom.DefaultConcepts.IDataStructureReadParameters], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = IntegerCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = IntegerDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LegacyEntityOrmMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LegacyEntityWithAutoCreatedViewOrmMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoadDslModelCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.LoadDslModelCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoadDslModelCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.LoadDslModelCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoadDslModelCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LongStringCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LongStringDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MetadataWorkspaceFileProvider (ReflectionActivator), Services = [Rhetos.Persistence.IMetadataWorkspaceFileProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MoneyCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MoneyDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceStorage (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorage], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceStorageObjectMappings (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorageObjectMappings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction, Rhetos.IUnitOfWork], Lifetime = Autofac.Core.Lifetime.MatchingScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransactionOptions (DelegateActivator), Services = [Rhetos.Utilities.PersistenceTransactionOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PingCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.PingCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PingCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PolymorphicOrmMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PrincipalWriter (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.PrincipalWriter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PropertyDatabaseColumnNameMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReadCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.ReadCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReadCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.ReadCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReadCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReferenceCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReferenceDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ReferencePropertyDatabaseColumnNameMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RegisteredInterfaceImplementations (ProvidedInstanceActivator), Services = [Rhetos.Dom.DefaultConcepts.RegisteredInterfaceImplementations], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RequestAndGlobalCache (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.Authorization.RequestAndGlobalCache], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SaveCommandClaims (ReflectionActivator), Services = [Rhetos.Security.IClaimProvider, Rhetos.Processing.DefaultCommands.SaveEntityCommandInfo (Rhetos.Security.IClaimProvider)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SaveEntityCommand (ReflectionActivator), Services = [Rhetos.Processing.ICommandImplementation, Rhetos.Processing.DefaultCommands.SaveEntityCommandInfo (Rhetos.Processing.ICommandImplementation)], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SaveEntityCommandInfo (ReflectionActivator), Services = [Rhetos.Processing.ICommandInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ServerCommandsUtility (ReflectionActivator), Services = [Rhetos.Processing.DefaultCommands.ServerCommandsUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ShortStringCsPropertyType (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ShortStringDatabaseColumnTypeMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlCommandBatch (ReflectionActivator), Services = [Rhetos.Dom.DefaultConcepts.IPersistenceStorageCommandBatch], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlObjectsIndex (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlQueryableOrmMetadata (ReflectionActivator), Services = [Rhetos.Dsl.IConceptMetadataExtension], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlResourcesProvider (ReflectionActivator), Services = [Rhetos.ISqlResources], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.ISqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = UnitOfWorkFactory (ReflectionActivator), Services = [Rhetos.UnitOfWorkFactory, Rhetos.IUnitOfWorkFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
";
    }
}