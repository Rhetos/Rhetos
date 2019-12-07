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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Dsl;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeployPackages.Test
{
    [TestClass]
    public class AutofacConfigurationTest
    {
        private readonly IConfigurationProvider _configurationProvider;
        public AutofacConfigurationTest()
        {
            _configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();

            LegacyUtilities.Initialize(_configurationProvider);
        }

        [TestMethod]
        public void CorrectRegistrationsDeployTime()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, new NLogProvider())
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsDeploy, registrationsDump);

                // in this scenario, IDslModel contains two registrations and it should resolve to DslModel, NOT DslModelFile
                var dslModel = container.Resolve<IDslModel>();
                Assert.AreEqual(typeof(DslModel), dslModel.GetType());
            }
        }

        [TestMethod]
        public void CorrectRegistrationsDeployTimeDatabaseOnly()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, new NLogProvider())
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            builder.RegisterInstance(new DeployOptions() { DatabaseOnly = true });

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsDeployDatabaseOnly, registrationsDump);

                // in this scenario, IDslModel contains two registrations and it should resolve to DslModelFile, NOT DslModel
                var dslModel = container.Resolve<IDslModel>();
                Assert.AreEqual(typeof(DslModelFile), dslModel.GetType());
            }
        }

        [TestMethod]
        public void CorrectRegistrationsRuntimeWithInitialization()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, new NLogProvider())
                .AddApplicationInitialization()
                .AddRhetosRuntime()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsRuntimeWithInitialization, registrationsDump);
            }
        }

        /// <summary>
        /// Server runtime registrations are based off DefaultAutofacRegistration module
        /// </summary>
        [TestMethod]
        public void CorrectRegistrationsServerRuntime()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, new NLogProvider())
                .AddRhetosRuntime();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsServerRuntime, registrationsDump);
            }
        }

        private string DumpSortedRegistrations(IContainer container)
        {
            var registrations = container.ComponentRegistry.Registrations
                    .Select(registration => registration.ToString())
                    .OrderBy(text => text)
                    .ToList();

            return string.Join(Environment.NewLine, registrations);
        }

        private static readonly string _expectedRegistrationsDeploy =
@"Activator = ApplicationGenerator (ReflectionActivator), Services = [Rhetos.Deployment.ApplicationGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AssemblyGenerator (ReflectionActivator), Services = [Rhetos.Compiler.IAssemblyGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CodeBuilder (ReflectionActivator), Services = [Rhetos.Compiler.ICodeBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CodeGenerator (ReflectionActivator), Services = [Rhetos.Compiler.ICodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptApplicationRepository (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptApplicationRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationExecuter (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDataMigrationExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseCleaner (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseCleaner], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseDeployment (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseDeployment], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGenerator (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGeneratorOptions (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseGeneratorOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModel (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScripts (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScripts], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFromDisk (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DeployOptions (ProvidedInstanceActivator), Services = [Rhetos.Utilities.DeployOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DiskDslScriptLoader (ReflectionActivator), Services = [Rhetos.Dsl.IDslScriptsProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomGenerator (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomGeneratorOptions (DelegateActivator), Services = [Rhetos.Dom.DomGeneratorOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModel (ReflectionActivator), Services = [Rhetos.Dsl.DslModel], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelFile], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslParser (ReflectionActivator), Services = [Rhetos.Dsl.IDslParser], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkMappingGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GeneratedFilesCache (ReflectionActivator), Services = [Rhetos.Utilities.GeneratedFilesCache], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = IDslModel (DelegateActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InitializationConcept (ReflectionActivator), Services = [Rhetos.Dsl.IConceptInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ReflectionActivator), Services = [Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MacroOrderRepository (ReflectionActivator), Services = [Rhetos.Dsl.IMacroOrderRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = NullImplementation (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDatabaseDefinition], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (ProvidedInstanceActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Tokenizer (ReflectionActivator), Services = [Rhetos.Dsl.Tokenizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WcfWindowsUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        private static readonly string _expectedRegistrationsDeployDatabaseOnly =
@"Activator = ApplicationGenerator (ReflectionActivator), Services = [Rhetos.Deployment.ApplicationGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AssemblyGenerator (ReflectionActivator), Services = [Rhetos.Compiler.IAssemblyGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CodeBuilder (ReflectionActivator), Services = [Rhetos.Compiler.ICodeBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CodeGenerator (ReflectionActivator), Services = [Rhetos.Compiler.ICodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptApplicationRepository (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptApplicationRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationExecuter (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDataMigrationExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseCleaner (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseCleaner], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseDeployment (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseDeployment], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGenerator (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGeneratorOptions (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseGeneratorOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModel (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScripts (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScripts], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFromDisk (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DeployOptions (ProvidedInstanceActivator), Services = [Rhetos.Utilities.DeployOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DeployOptions (ProvidedInstanceActivator), Services = [Rhetos.Utilities.DeployOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DiskDslScriptLoader (ReflectionActivator), Services = [Rhetos.Dsl.IDslScriptsProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomGenerator (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomGeneratorOptions (DelegateActivator), Services = [Rhetos.Dom.DomGeneratorOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModel (ReflectionActivator), Services = [Rhetos.Dsl.DslModel], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelFile], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslParser (ReflectionActivator), Services = [Rhetos.Dsl.IDslParser], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkMappingGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GeneratedFilesCache (ReflectionActivator), Services = [Rhetos.Utilities.GeneratedFilesCache], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = IDslModel (DelegateActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InitializationConcept (ReflectionActivator), Services = [Rhetos.Dsl.IConceptInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ReflectionActivator), Services = [Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MacroOrderRepository (ReflectionActivator), Services = [Rhetos.Dsl.IMacroOrderRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = NullImplementation (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDatabaseDefinition], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (ProvidedInstanceActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Tokenizer (ReflectionActivator), Services = [Rhetos.Dsl.Tokenizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WcfWindowsUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        private static readonly string _expectedRegistrationsRuntimeWithInitialization =
@"Activator = ApplicationInitialization (ReflectionActivator), Services = [Rhetos.Deployment.ApplicationInitialization], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DeployOptions (ProvidedInstanceActivator), Services = [Rhetos.Utilities.DeployOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GeneratedFilesCache (ReflectionActivator), Services = [Rhetos.Utilities.GeneratedFilesCache], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ReflectionActivator), Services = [Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (ProvidedInstanceActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WcfWindowsUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        private static readonly string _expectedRegistrationsServerRuntime =
@"Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GeneratedFilesCache (ReflectionActivator), Services = [Rhetos.Utilities.GeneratedFilesCache], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ReflectionActivator), Services = [Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (ProvidedInstanceActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WcfWindowsUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";
    }
}
