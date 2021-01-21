# Rhetos release notes

## 5.0.0 (TO BE RELEASED)

### Breaking changes

1. Rhetos no longer supports .NET Framework.
2. Removed WcfWindowsUserInfo, IWindowsSecurity and WindowsSecurity from Rhetos framework. WindowsSecurity property on ExecutionContext is removed.
   * TODO: These classes might be implemented in a separate plugin package.
3. Removed CleanupOldData executable
4. Removed CreateAndSetDatabase executable.
   * Use [sqlcmd](https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility) CLI utility to create a database.
5. Removed CreateIISSite executable
6. Removed DeployPackages executable
   * rhetos.exe is replacing DeployPackages.exe, see [Migrating from DeployPackages to Rhetos CLI](https://github.com/Rhetos/Rhetos/wiki/Migrating-from-DeployPackages-to-Rhetos-CLI).
7. Removed support for WCF in Rhetos framework. That means that Rhetos.Web assembly is also removed from the Rhetos framework.
8. Removed PackageDownloader and PackageDownloaderOptions class because the NuGet package management is done by MSBuild or dotnet CLI.
9. Removed IHomePageSnippet from the Rhetos framework
    * TODO: The support for IHomePageSnippet will be probably left to a rhetos plugin that will support the ASP.NET Core.
10. Removed IService from the Rhetos framework
    * TODO: A new design is needed to support custom services in ASP.NET Core.
11. SOAP API implementation is removed from Rhetos framework (IServerApplication).
12. Rhetos.TestCommon.dll is moved to a separate NuGet package.
    * If your build fails with error `......`, add a NuGet package reference to "Rhetos.TestCommon".
13. Removed LINQPad scripts from the Rhetos framework.
    * TODO: Implement them in a separate NuGet package.
14. TestUtility.DumpSorted may return items in a different order, because the default string comparer is different between .NET Framework and .NET 5.
15. Removed LegacyPathsOptions class, specific for build process with DeployPackages.
16. Removed methods ICodeBuilder.AddReference and AddReferencesFromDependency.
    * Calls to this methods can be removed from custom code, since since Rhetos no longer compiles assemblies directly.
17. Removed IAssemblySource. ICodeGenerator.ExecutePlugins returns generated source as a string.
    * Custom code that called ExecutePlugins can use the string result directly, instead of IAssemblySource.GeneratedCode property.
    * Code that used IAssemblySource.RegisteredReferences can be safely removed, since Rhetos no longer compiles assemblies.
18. Removed Plugins class, use ContainerBuilderPluginRegistration instead. Resolve it from ContainerBuilder with extension method builder.GetPluginRegistration().
19. Removed support for source code compilation. Removed IAssemblyGenerator.
    Removed options Debug and AssemblyGeneratorErrorReportLimit from Build options.
    * Custom code that used IAssemblyGenerator.Generate to generate application's source and library should now use ISourceWriter.Add instead. This will simply add the generated source files into the project that will be compiled as a part of the Rhetos application.
    * If you need to compile the generated source code to a separate library, include the generated source code inside a separate C# project and leave the compilation to Visual Studio or MSBuild.
    20. The property DatabaseLanguage is now located in the class DatabaseSettings instead of BuildOptions during build time and RhetosAppOptions during runtime.

## 4.3.0 (TO BE RELEASED)

### New features

* **DateTime** property concept can now create *datetime2* database column type, instead of obsolete *datetime* column type (issue #101). Legacy *datetime* type is currently enabled by default, for backward compatibility. See [Migrating an existing application from datetime to datetime2](https://github.com/Rhetos/Rhetos/wiki/Migrating-from-DateTime-to-DateTime2).
* Custom ID value can be specified for Entry of a **Hardcoded** entity (see [documentation](https://github.com/Rhetos/Rhetos/wiki/simple-read-only-entities-and-codetables)).

### Internal improvements

* Saving records directly to database, instead of using Entity Framework (issue #374).
  * Entity Framework is still used for querying data, and EF DbContext may still be used for writing to database where specifically needed.
  * This change allows for different performances optimizations and simpler internal design,
    because of a mismatch in write approach between Rhetos and Entity Framework
    (developers use explicit insert/update/delete operations in Rhetos).
* Bugfix: Rhetos build was sometimes not triggered by MSBuild, if an input file was deleted.
* Bugfix: Some database schemas were created with incorrect owner (an admin account that created the schema, instead of dbo), depending on database configuration (issue #92). Note that this is not an application security issue. On older Rhetos versions it might have caused database update to fail, if a database schema needed to be dropped.
* Bugfix: Incorrect "method is obsolete" warning on Load method, with description "Use Load(ids) or Query(ids) method.".

## 4.2.0 (2020-10-26)

### Internal improvements

* Build performance improvements: Various optimizations in different segments of build process, simplified generated code, reduced usage of macro concepts for database object dependencies, reduced unneeded changes in ordering of the generated code.
* Bugfix: Build sometime fails with EntityCommandCompilationException "The argument type 'X' is not compatible with the property 'Y' of formal type 'Z'.".
* Reduced logging level for certain build and performance events from *warning* to *info*.
* A redundant FilterBy is no longer generated for each ComposableFilterBy.

## 4.1.0 (2020-09-23)

### New features

* **QueryFilter** concept, a simpler alternative to ComposableFilterBy.

### Internal improvements

* "Long operation" warning will be reported on deployment if certain operations take longer than a minute (by default).
  This includes KeepSynchronized on deployment, data-migrations scripts, database updates (dropping a column, e.g.)
  and other similar scripts (issue #110).
* Reduced compile time of generated C# source code in repository classes by removing
  some code duplication and converting some lambda expressions to simple methods.
* SQL optimization: Removed parameter null check on a generated SQL query when using generic filters to compare
  a Guid property to parameter value.
  Applications with EntityFrameworkUseDatabaseNullSemantics set to true (default) had this optimization from before.
* Improved application start-up performance with Entity Framework View Cache (issue #165).
* Improved build performance by running code generator in parallel.
* Bugfix: "File not found" error occurred only on empty build (for example, without the CommonConcepts package).
* Base concepts for loading and querying data with filters: LoadInfo, QueryInfo, FilterInfo, QueryFilterInfo.
  Together they represent available filter parameters for reading data with generic filters (FilterCriteria),
  web API (ReadCommand), and GenericRepository.
* Performance logger (NLog) configurable by class name.

## 4.0.1 (2020-06-19)

### Internal improvements

* Bugfix: Downgrade from Rhetos v4.0.0 to previous versions fails with SqlException on *DatabaseGeneratorAppliedConcept* table (issue #353).

## 4.0.0 (2020-05-14)

### Breaking changes

1. Rhetos has migrated to new configuration and DI container initialization design.
   Custom utility applications (executables and other tools) that use Rhetos application
   libraries will need to be updated.
   * Follow the **instructions** at [Upgrading custom utility applications to Rhetos 4.0](https://github.com/Rhetos/Rhetos/wiki/Upgrading-custom-utility-applications-to-Rhetos-4).
2. Class `Rhetos.Configuration.Autofac.SecurityModuleConfiguration` no longer exists.
   * **Delete** attributes `[ExportMetadata(MefProvider.DependsOn, typeof(Rhetos.Configuration.Autofac.SecurityModuleConfiguration))]`
     and ``[ExportMetadata(MefProvider.DependsOn, typeof(SecurityModuleConfiguration))]`` from your code.
3. The `IQueryable` extension method `ToSimple()` moved from Rhetos.Dom.DefaultConcepts
   namespace to System.Linq.
   * **Add** `using System.Linq;` in custom source file with compiler error
     `'IQueryable<X>' does not contain a definition for 'ToSimple'`.
4. Configuration keys have been changed for some of the existing options, to allow better extensibility.
   * **Update** the following settings keys in *Web.config* file and in all **other config files** that contain the `appSettings` element,
     or **enable** legacy keys by adding `<add key="Rhetos:ConfigurationProvider:LegacyKeysSupport" value="Convert" />`.
     * AssemblyGenerator.ErrorReportLimit => Rhetos:Build:AssemblyGeneratorErrorReportLimit
     * AuthorizationAddUnregisteredPrincipals => Rhetos:App:AuthorizationAddUnregisteredPrincipals
     * AuthorizationCacheExpirationSeconds => Rhetos:App:AuthorizationCacheExpirationSeconds
     * BuiltinAdminOverride => Rhetos:AppSecurity:BuiltinAdminOverride
     * CommonConcepts.Debug.SortConcepts => Rhetos:Build:InitialConceptsSort
     * CommonConcepts.Legacy.AutoGeneratePolymorphicProperty => CommonConcepts:AutoGeneratePolymorphicProperty
     * CommonConcepts.Legacy.CascadeDeleteInDatabase => CommonConcepts:CascadeDeleteInDatabase
     * DataMigration.SkipScriptsWithWrongOrder => Rhetos:DbUpdate:DataMigrationSkipScriptsWithWrongOrder
     * EntityFramework.UseDatabaseNullSemantics => Rhetos:App:EntityFrameworkUseDatabaseNullSemantics
     * Security.AllClaimsForUsers => Rhetos:AppSecurity:AllClaimsForUsers
     * Security.LookupClientHostname => Rhetos:AppSecurity:LookupClientHostname
     * SqlCommandTimeout => Rhetos:Database:SqlCommandTimeout
     * SqlExecuter.MaxJoinedScriptCount => Rhetos:SqlTransactionBatches:MaxJoinedScriptCount
     * SqlExecuter.MaxJoinedScriptSize => Rhetos:SqlTransactionBatches:MaxJoinedScriptSize
     * SqlExecuter.ReportProgressMs => Rhetos:SqlTransactionBatches:ReportProgressMs
5. Some legacy settings are turned off by default. When upgrading application to Rhetos v4,
   make sure to use the following setting values *web.config* file.
   For each setting key, if it was already specified in *web.config*, **keep the old value**.
   If you did not have it specified, **add** the backward-compatible value provided here:
   * `<add key="Rhetos:DbUpdate:DataMigrationSkipScriptsWithWrongOrder" value="True" />`
   * `<add key="CommonConcepts:AutoGeneratePolymorphicProperty" value="True" />`
   * `<add key="CommonConcepts:CascadeDeleteInDatabase" value="True" />`
   * `<add key="Rhetos:App:EntityFrameworkUseDatabaseNullSemantics" value="False" />`
6. Updated Rhetos.TestCommon.dll to new MSTest libraries (MSTest).
   Old unit test projects that reference Rhetos.TestCommon may fail with error CS0433:
   `The type 'TestMethodAttribute' exists in both 'Microsoft.VisualStudio.QualityTools.UnitTestFramework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' and 'Microsoft.VisualStudio.TestPlatform.TestFramework, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'`.
   * To solve this error, in the unit test project **remove the reference** to
     `Microsoft.VisualStudio.QualityTools.UnitTestFramework` and **add NuGet packages**
     MSTest.TestAdapter and MSTest.TestFramework, or create a new Unit Test Project
     from scratch (MSTest on .NET Framework) and copy the tests from the old one.
7. Partial concept sorting enabled by default. This might cause build or deployment
   to fail if the existing project or a plugin package has an issue of missing
   dependencies between concepts, that was not detected earlier.
   * Missing dependencies can cause an error on database update because of incorrect order
     of created objects (typically `Invalid column name` or `Invalid object name`),
     or `Generated script does not contain tag` error in code generators.
   * If *DeployPackages* fails with some of errors above, **add** missing dependencies
     to [custom concepts](https://github.com/Rhetos/Rhetos/wiki/Rhetos-concept-development#dependency-between-code-generators)
     and [database object](https://github.com/Rhetos/Rhetos/wiki/Database-objects#dependencies-between-database-objects),
     or **suppress** this issues by disabling the concept sorting in Web.config:
     `<add key="Rhetos:Build:InitialConceptsSort" value="None" />`.
8. [BuiltinAdminOverride](https://github.com/Rhetos/Rhetos/wiki/Basic-permissions#suppressing-permissions-in-a-development-environment)
   option is not enabled by default. This might affect testing in development
   environment if permission-checking was intentionally suppressed.
   * Recommended setup for development environment is to **configure**
     `Rhetos:AppSecurity:AllClaimsForUsers` in `ExternalAppSettings.config`
     (see [Suppressing permissions in a development environment](https://github.com/Rhetos/Rhetos/wiki/Basic-permissions#suppressing-permissions-in-a-development-environment)),
     or enable BuiltinAdminOverride for backward-compatibility in Web.config:
     `<add key="Rhetos:AppSecurity:BuiltinAdminOverride" value="True" />`.
9. ProcessUserInfo no longer supports BuiltinAdminOverride configuration option.
   This might affect unit tests or custom utilities that directly use
   Rhetos.Processing.IProcessingEngine.
   * Add the required permissions for the system account that runs the utility application.
   * Alternatively, a unit test can register NullAuthorizationProvider to DI container,
     if it needs to override end-user permissions checking.
     For example, if using RhetosTestContainer add
     `container.InitializeSession += builder.RegisterType<Rhetos.Security.NullAuthorizationProvider>().As<IAuthorizationProvider>();`
10. For custom plugins that implement *IGenerator* interface, it is no longer assumed to have
    dependency to ServerDom.*.dll libraries or Resources folder. Such plugin might be executed
    *before* ServerDom libraries or Resources folder is generated, if the dependency is not specified
    by IGenerator.Dependencies property.
    * If a custom IGenerator plugin reads files from **Resources folder** at build-time, the Dependencies property should include `typeof(Rhetos.Deployment.ResourcesGenerator).FullName`.
    * If it requires **ServerDom.*.dll** at build-time, the Dependencies property should include `typeof(Rhetos.Dom.DomGenerator).FullName"`.
      Missing dependency may results with C# compiler error "The type or namespace name 'DomRepository' could not be found", or similar.

### New features

* **Rhetos CLI and MSBuild integration**
  * Rhetos CLI (rhetos.exe) is a successor to DeployPackages. The old build process with DeployPackages.exe still works.
  * It allows custom source code to be developed and compiled side-by-side with generated
    C# source code in the same Rhetos application.
  * New Rhetos application are created and developed as standard web application in Visual Studio
    by adding Rhetos NuGet packages. Currently only WCF applications are supported.
  * To migrate an existing Rhetos application to new build process follow the instructions at
    [Migrating from DeployPackages to Rhetos CLI](https://github.com/Rhetos/Rhetos/wiki/Migrating-from-DeployPackages-to-Rhetos-CLI).
* **Rhetos.MSBuild** NuGet package for integration with MSBuild and Visual Studio.
* **Rhetos DSL IntelliSense for Visual Studio** provides syntax highlighting,
  context-sensitive autocompletion, concept description with parameters and real-time
  syntax errors. See installation instructions and features at
  [Rhetos/LanguageServices](https://github.com/Rhetos/LanguageServices/blob/master/README.md).
* **Rhetos.Wcf** NuGet package for quick-start of new WCF application projects (contains Rhetos.MSBuild).
  See [Creating new WCF Rhetos application](https://github.com/Rhetos/Rhetos/wiki/Creating-new-WCF-Rhetos-application).

### Internal improvements

* Added summaries to all DSL concepts in CommonConcepts. They are displayed in Rhetos DSL IntelliSense for Visual Studio.
* ProcessContainer is a successor to RhetosTestContainer with cleaner lifetime and transaction handling. See examples in [Upgrading custom utility applications to Rhetos 4.0](https://github.com/Rhetos/Rhetos/wiki/Upgrading-custom-utility-applications-to-Rhetos-4) and in "Rhetos Server DOM.linq" script.
* Better support for anonymous access and testing with new security option [Rhetos:AppSecurity:AllClaimsForAnonymous](https://github.com/Rhetos/Rhetos/wiki/Basic-permissions#suppressing-permissions-in-a-development-environment).
* Database update is logging modified database objects information by default. This will help with analysis of deployment performance issues.
* Bugfix: `DeployPackages /DatabaseOnly` requires packages source available to update the database (PackagesCache or source folders). If the application was built with packages included directly from source folder, it could not be deployed with DatabaseOnly switch.
* Bugfix: Trace logging fails on some server commands because of result types unsupported by XML serializer (e.g. ODataGenerator).
* Rhetos configuration can be extended with [custom options classes](https://github.com/Rhetos/Rhetos/wiki/Configuration-management#reading-configuration-with-custom-options-classes).
* ConnectionStrings.config no longer needs to be a separate file in the 'bin' folder. It is still recommended to use a separate file, but place it in the application root folder, to avoid deleting it when rebuilding bin folder. Note that its location must be specified in Web.config file.
* Removed Rhetos-specific database providerName from connection string.
* Verifying and updating EntityFramework ProviderManifestToken on each runtime startup. This allows different versions of SQL Server in build and testing environment (local SQL Server, Azure SQL, ...).
* New logging level: *Warning*. Simplified logging rules in Web.config.
* Low-level concept *CascadeDeleteInDatabase*, for implementing on-delete-cascade in database
  on specific FK constraint (Reference, UniqueReference and Extends).
  It should be rarely used because deleting records directly in database circumvents
  any business logic implemented in the application, related to those records.
* Homepage snippets can now use IUserInfo and other DI lifetime scope components.
* Migrated most of the Rhetos framework libraries to .NET Standard 2.0.
* Formatting DSL syntax errors in canonical format (file and position) for better integration with MSBuild and other tools.
* Rhetos NuGet package was missing System.ComponentModel.Composition dependency.
* Updated Rhetos framework C# language to 7.3. Framework development now requires Visual Studio 2017 v15.7 or later.
* Minor performance improvements of DeployPackages and application start-up.

## 3.2.1 (2020-11-19)

The changes from this release are not included in versions 4.0.0 - 4.2.0.

### Internal improvements

* Bugfix: Read command with row permissions filter is not be optimized in some situations, resulting with an additional database query.

## 3.2.0 (2020-08-20)

The changes from this release are not included in versions 4.0.0 - 4.0.1. They are included in 4.1.0 and later.

### Internal improvements

* Repository Save method is now *virtual*, to allow simpler unit testing mocks.

## 3.1.0 (2020-06-05)

### New features

* DSL syntax: ConceptParentAttribute, for explicit specification of parent property for nested and recursive concepts.
  The first property is assumed to be parent concept by default, unless ConceptParentAttribute is used.
  **Recursive concepts** must use ConceptParentAttribute.
* DSL syntax: Dot is required as a parameter separator *only* for referenced concepts.
  In previous versions, dot was also required as a separator before key properties
  of type string.
  For backward compatibility, both old and new syntax is currently allowed (configurable).
  * For example, when using *flat* syntax for ShortString, previous versions required
    dot before property name: `ShortString Demo.School.Name;`, but new version does not:
    `ShortString Demo.School Name;`.
    New version makes it more clear that ShortString contains 2 parameters:
    entity (`Demo.School`) and property name (`Name`).

### Internal improvements

* Bugfix: Optimized "Contains" query with GroupBy and subquery results with
  `EntityCommandExecutionException: An error occurred while executing the command definition.`
  with inner exception
  `IndexOutOfRangeException: Invalid index -1 for this SqlParameterCollection with Count=0.` (issue #278)
* Bugfix: Downgrade from Rhetos 4.0 results with SQL error:
  `Cannot find the object "Rhetos.DatabaseGeneratorAppliedConcept" because it does not exist or you do not have permissions.`
* Bugfix: KeepSynchronized metadata tracking can result with syntax error in
  *ServerDom.Repositories.cs* depending on concept ordering.

## 3.0.1 (2019-11-22)

### Internal improvements

* Bugfix: GenericFilter "notequal" and "notequals" translates to Expression.Equal (issue #225).

## 3.0.0 (2019-11-07)

### Breaking changes

1. Upgrade from .NET Framework 4.5.1 to .NET Framework 4.7.2 (issue #52).
   * **Update *Web.config*** in your Rhetos server application:
     replace *multiple* instances of `targetFramework="4.5.1"` with `targetFramework="4.7.2"`.
   * If your Visual Studio project *directly references* ServerDom dlls,
     **change the *Target framework*** in project properties to ".NET Framework 4.7.2":
     In Visual Studio right-click on your project, select Properties, select the Application tab,
     change the Target framework to the ".NET Framework 4.7.2".
     If you are not seeing .NET Framework 4.7.2 as an option there, ensure you have it installed.
   * If you have a *.nuspec* file that contains your project's dll,
     **replace** `target="lib\net451"` with `target="lib\net472"` in the .nuspec.
2. Upgraded project dependencies to the latest version:
   Autofac 3.5.2 to 4.9.4,
   Autofac.Wcf 3.0.1 to 4.1.0,
   NLog 4.5.4 to 4.6.7,
   NuGet.Core 2.8.3 to 2.14.0
   and Newtonsoft.Json 6.0.8 to 12.0.2.
   * To allow your existing Rhetos application to work with existing plugin packages,
     without recompiling them with new version of the dependencies,
     you will need to add `bindingRedirect` configuration in the .config file.
     **See the instructions at** [Using old packages with new NuGet dependencies](https://github.com/Rhetos/Rhetos/wiki/Using-old-packages-with-new-NuGet-dependencies).
     If the `bindingRedirect` is missing, the application will return
     one of the following errors on startup:
       * *System.InvalidCastException: Unable to cast object of type '...' to type 'Autofac.Module'.*
       * *Could not load file or assembly 'Autofac, Version=3.3.0.0, ...*
       * *ReflectionTypeLoadException: Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.*
3. Removed dependency to Autofac.Configuration and DotNetZip (Ionic.Zip).
   * If your application or plugin package requires one of the dependencies,
     add the dependency specification in the .nuspec file.
4. Default EF storage model namespace changed from "Rhetos" to "Rhetos.Store".
   * If your application contains a `IConceptMapping` plugin that creates EF metadata for custom SQL function
     at `EntityFrameworkMapping.StorageModelTag`, then modify the DbFunction attribute on
     the related C# method from `"Rhetos"` to `EntityFrameworkMapping.StorageModelNamespace`.
     Note that this should be done only for *StorageModelTag* functions, not *ConceptualModelTag*.

### New features

* System-managed roles: "AllPrincipals" and "Unauthenticated".
  They simplify assigning permissions for all users. More info available in article
  [Basic permissions](https://github.com/Rhetos/Rhetos/wiki/Basic-permissions#system-roles-allprincipals-and-anonymous).
* C# 6.0 and C# 7.0 features are now available in DSL code snippets (issue #52).

### Internal improvements

* Using UTF-8 by default to read .rhe and .sql files. Fallback to default system encoding in case of an error. (issue #163)
* Deployment performance: Using Roslyn compiler for generated dll files.
* Deployment performance: Custom Entity Framework metadata generator instead of automatically generated by Code First.
* Bugfix: NuGet download results with error "... already has a dependency defined for 'NETStandard.Library'" (issue #215).
* Bugfix: Deployment error MissingMethodException: Method not found Common.Queryable... (issue #178).
* Bugfix: Multiple ModificationTimeOf on same entity may not detect some indirect property changes.
* Bugfix: Corrected backward compatibility issue for SamePropertyValue (since v2.11).
* AutodetectSqlDependencies now detects INSERT INTO, MERGE and JOIN query parts.
* Improved error reporting:
  explanatory Web API response for invalid JSON format,
  cause analysis for type load errors,
  warnings on empty build, ...
* DSL parser disambiguation for flat syntax over nested syntax (issue #210).
* Simplified DSL syntax of polymorphic implementation that references a hardcoded entry (see [usage](https://github.com/Rhetos/Rhetos/wiki/simple-read-only-entities-and-codetables#usage-in-polymorphic-implementation)).
* Runtime performance: Entity Framework query optimizations for better reuse of query cache
  in both EF and database (issues #169 and #167).
* Runtime performance: Recommended value `EntityFramework.UseDatabaseNullSemantics` set to True for new applications
  (see [recommendations](https://github.com/Rhetos/Rhetos/wiki/Migrating-an-existing-application-to-UseDatabaseNullSemantics)).

## 2.12.0 (2019-09-09)

### New features

* New concepts: **Hardcoded**, **Entry** and **Value**, for read-only entities in DSL scripts (code tables, for example).

### Internal improvements

* New low-level concepts: **ArgumentValidation** and **OldDataLoaded**,
  for extending [SaveMethod](https://github.com/Rhetos/Rhetos/wiki/Low-level-object-model-concepts).
* Bugfix: AutoCodeForEach on Bool grouping property causes unique index error on insert (issue #132).
* Bugfix: Multiple row permissions with subqueries can result with ArgumentException (issue #191).
* AutoInheritRowPermissions now safely ignores self-references (issue #35).
* Bugfix: Backup data-migration column is dropped when converting table to view (issue #170).
* Simpler error summary for better readability in VS Code terminal (issue #175).
* Bugfix: DslScripts and DataMigration folders are ignored when deploying a package *from source*,
  if they are not placed directly under the *nuspec* file (issue #173).
* Minor performance optimizations (reading and writing data, DSL parser, database upgrade)
  and improvements in error handling.
* New plugin type: IConceptDataMigration, for custom data-migration extensions (*Hardcoded* concept, for example).
* New plugin type: IConceptMetadataExtension, for providing information on concept implementation details (database column type, for example).

## 2.11.1 (2019-05-23)

### Internal improvements

* Bugfix: "The DELETE statement conflicted with the REFERENCE constraint" error when deleting a record that is included in a polymorphic,
  event when the cascade delete should be applied automatically (issue #154).

## 2.11.0 (2019-01-31)

### New features

* New concept **SkipRecomputeOnDeploy**, for disabling recompute-on-deploy for a specific entity in the DSL script (issue #105).
* Additional full-text search features, available as overloads of the existing **FullTextSearch** LINQ query method:
  1. Alternative integer key instead of GUID ID.
  2. Limiting the search subquery results with
     [top_n_by_rank](https://docs.microsoft.com/en-us/sql/relational-databases/system-functions/containstable-transact-sql?view=sql-server-2017).
* New low-level concept **RepositoryMember**, for extending generated repository classes (issue #87).
* Generating .xml documentation files for the class libraries (issue #100).

### Internal improvements

* Simplified syntax of concept **SamePropertyValue** to use a simple path,
  see [RowPermissions](https://github.com/Rhetos/Rhetos/wiki/RowPermissions-concept) example.
* Bugfix: **SqlDependsOn** referencing polymorphic data structure did not create a dependency to the polymorphic's view (issue #98).
* Cascade delete is executed only in the application layer, not in the database, to prevent bypassing any business rules (issue #90).
  The new behavior can be enabled in *old applications* by setting
  the [appSettings](https://github.com/Rhetos/Rhetos/blob/v2.1.0/Source/Rhetos/Web.config) option
  "CommonConcepts.Legacy.CascadeDeleteInDatabase" to "False".
* Minor performance improvements for DSL parser.

## 2.10.0 (2018-12-06)

### New features

* New concept: **DefaultValue**, for setting the default property values when inserting a new record.
  See usage [examples](https://github.com/ibarban/Rhetos/blob/bc023542c4a7d5d117bf74bdac9a75f2ffe95c12/CommonConcepts/CommonConceptsTest/DslScripts/DefaultValueTest.rhe).
* New *DeployPackages.exe* switch: `/SkipRecompute`, to disable automatic updating of the computed data marked with KeepSynchronized during deployment (issue #83).

### Internal improvements

* Bugfix: Cascade delete for extension bypasses the entity's object model.
* Bugfix: Cannot run CreateAndSetDatabase outside bin folder (issue #67).
* Turned off automatic generated properties of polymorphic subtype, since it was easy to forget to implement a new subtype property (issue #62). An error is reported instead.
  Note: To apply the new behavior to the existing applications, add `<add key="CommonConcepts.Legacy.AutoGeneratePolymorphicProperty" value="False" />` to *Web.config*.
* Performance: Improved NuGet package caching.
* `EntityFramework.UseDatabaseNullSemantics` setting can be changed without re-deploying the application (issue #64).

## 2.9.0 (2018-09-21)

### Internal improvements

* Bugfix: Deployment with /DatabaseOnly fails with SqlException: Invalid column name 'Subtype' (since 2.8.0).
* Bugfix: When trying to delete entity that has a UniqueReference entity without CascadeDelete concept applied, insufficient error message is returned. There is no info about entity that references aforementioned entity.

## 2.8.0 (2018-09-17)

### Internal improvements

* Bugfix: DeployPackages sometimes fails with InvalidOperationException: This SqlTransaction has completed; it is no longer usable.
* Bugfix: DeployPackages sometimes fails with DslSyntaxException: "Concept with same key is described twice with different values." for PolymorphicPropertyInfo.
* All security plugins (IUserInfo/IUserInfoAdmin) can now provide the admin user information. Previously this option was available only to Windows authentication.
* **ApplyFilterOnClientRead** concept: Added an optional selector of read commands where the filter will be applied.

## 2.7.0 (2018-06-27)

### Internal improvements

* Bugfix: The **ModificationTimeOf** concept sometime sets the current time value before the **CreationTime** concept on the same data entry.
* Bugfix: The **PessimisticLocking** concept uses Rhetos server time instead of the database time.
* Bugfix: "Conflicting source files with different file size" error when downloading localized NuGet packages.
* Improved database deployment over networks with high latency: performance improvements, progress reporting.
* Minor improvements in error reporting and logging (most notably for data migration scripts).

## 2.6.1 (2018-06-04)

### Internal improvements

* Corrected the default log archive size.

## 2.6.0 (2018-06-01)

### Internal improvements

* Performance: Enabled parallel inserts on **AutoCodeForEach** for different groups or records.
* Performance: New low-level concept **LogReaderAdditionalSource**, that enables implementation of a custom Log archive. The archive can be integrated into the existing auditing features by extending *LogReader* and *LogRelatedItemReader*.
* Upgraded to NLog v4.5.4 from v3.2.0. Modified default NLog configuration to use the Logs subfolder, and archive the large log files. Fixed the *asyncwrapper* usage.

## 2.5.0 (2018-04-23)

### Internal improvements

* Bugfix: Server startup sometime fails with a NullReferenceException (since v2.0.0).

## 2.4.0 (2017-12-20)

### New features

* New concept: **AfterSave**, for actions that need to be executed *after* all save validations are done.

### Internal improvements

* Bugfix: Polymorphic "_Materialized" records should by automatically updated when a new subtype implementation is added or removed.
* Allowed use of Detail concept on non-writable data structures. This can be useful for inheriting row permissions, for example.
* Error handling improvements on deployment.

## 2.3.0 (2017-11-27)

### Internal improvements

* Bugfix: *DeployPackages* fails with a ProviderManifestToken error when migrating to a different SQL Server version.
* Bugfix: *DeployPackages* fails with an error when executing data-migration scripts:
  "View or function 'x' is not updatable because the modification affects multiple base tables."
* Bugfix: *DeployPackages* fails with a database error 'identifier too long' on foreign key constraint,
  when entity name's length is 40 characters. The limit is now moved above 100 characters,
  but it depends on other features and plugins that are used in the application.
* Increased EF6 query cache size to 10000 (from default 1000).
* Disabled ASP.NET session to improve concurrency. Rhetos server does not use it, but it was turned on by default.

## 2.2.0 (2017-10-18)

### Internal improvements

* Bugfix: `DeployPackages /DatabaseOnly` fails if the Rhetos server folder has been moved.

## 2.1.0 (2017-10-17)

### Internal improvements

* Bugfix: AdminSetup.exe fails with FileNotFoundException.
* Minor optimizations in performance critical code.
* New plugins can suppress existing plugins from collections in IPluginsContainer and INamedPlugins.
* ActiveDirectorySync package moved to a separate source repository:
  <https://github.com/Rhetos/ActiveDirectorySync>
* AspNetFormsAuth package is moved to a separate source repository:
  <https://github.com/Rhetos/AspNetFormsAuth>

## 2.0.0 (2017-10-02)

### Breaking changes

* *ServerDom.dll* moved from `bin` to `bin\Generated` and split to the following files: ServerDom.Repositories.dll, ServerDom.Model.dll, ServerDom.Orm.dll.
  * Please **delete** any remaining ServerDom.* files from the bin folder.
* When using **AllProperties** concept on **ComputedFrom** or **Persisted**, it is no longer allowed to override the target property name for the computed properties. Use an explicit property mapping instead.

### New features

* New concept: **UniqueReference**, similar to the **Extends** concepts but without cascade delete and row permissions inheritance.
* New concept: **CascadeDelete** for **UniqueReference**.

### Internal improvements

* Deployment performance: Caching generated assembly and EDM files.
* Run-time performance: Removed use of IObjectContextAdapter at Entity Framework initialization and saving.
* Run-time performance: Optimized row permissions that uses an extension when inherited to that extension.
* Bugfix: Inheriting row permissions results with a lambda expression error when using subquery parameters of the same type as the main expression parameter or when inheriting to an extension that is used in the main expression.

## 1.10.0 (2017-08-11)

### Internal improvements

* Bugfix: Updating or deleting a nonexistent records on entity with row permissions is misreported as "You are not authorized ..."
  instead of "Updating/Deleting a record that does not exist ...".
* Repository members are now available in **KeepSynchronized** filter snippet and **Computed** code snippet (*_executionContext*, for example).
* Included date and time to internal server error message to help finding the error record in the server log.

## 1.9.0 (2017-07-20)

### Internal improvements

* Performance improvements of *DeployPackages.exe*.
* Improved request logging: *ProcessingEngine Request* logger logs simplified description of server commands with filter parameters.

## 1.8.0 (2017-06-30)

### Internal improvements

* New concept: **AutoInheritRowPermissionsInternally**, similar to **AutoInheritRowPermissions** but it does not inherit row permissions from other modules.
* Bugfix: **KeyProperties** concept sometimes adds ID to the list of properties.
* One persisted property can be included in more than one **ComputedFrom** mapping.
* RestGenerator: The error "There is no resource of this type with a given ID" is no longer logged to RhetosServer.log by default.

## 1.7.0 (2017-03-09)

### New features

* Support for *Azure SQL Database*.

### Internal improvements

* Bugfix: Error when deploying to an empty database "There is already an object named 'Common' in the database".
* Bugfix: MetadataException when deploying on SQL Server 2008 "All SSDL artifacts must target the same provider".
* Removed DSL constraint "Browse should be created in same module as referenced entity".

## 1.6.0 (2017-02-27)

### New features

* New concept: **AllowSave**, for suppressing **InvalidData** validations on save.
  The `Validate` method is available on the repository to list the validation errors that do not block saving.
* New concept: **RequiredAllowSave**, for suppressing **Required** property validations on save.
* New concept: **ErrorMetadata**, for setting the extensible metadata to **InvalidData** error message.

### Internal improvements

* Bugfix: The *SqlCommandTimeout* configuration option is not applied to the Entity Framework queries.
* *AssemblyGenerator* uses UTF-8 with BOM when writing the source file (*ServerDom.cs*, for example).

## 1.5.0 (2017-02-13)

### New features

* New concept: **ChangesOnReferenced**, a helper for defining a computation dependency to the referenced entity.
  The parameter is a reference property name, or a path over references, pointing to the entity that the current computation depends on.
  Note that both computed data structure and the persisted data structure must have the same reference property for this concept to work.
  The path can include the *Base* reference from extended concepts.
  The path can target a **Polymorphic**. This will generate a **ChangesOnChangesItems** for each Polymorphic implementation.

### Internal improvements

* Improved logging of server commands.
  The log now contains a short command description, including the entity/action/report name.
  Logging full command description for failed commands; it can be configured separately by error severity (client or server errors) in *Web.config*.
* Bugfix: **AutodetectSqlDependencies** does not detect dependencies to the **Polymorphic**'s view.
* Optimized use of *FilterCriteria* in the *Recompute* functions.

## 1.4.0 (2016-12-13)

### Internal improvements

* Bugfix: NuGet package ID should be case insensitive.
* Relaxed validation of duplicate concepts in the DSL model.
  The validation will ignore macro concept that generates a base concept
  when a derived concept already exists.

## 1.3.0 (2016-12-08)

### New features

* New generic filter operation: **In** checks if the property's value is in the provided list.
  It is available in web APIs such as [REST service](https://github.com/Rhetos/RestGenerator).
  In business layer object model this property filter can be executed with a subquery parameter.
* New concept: **AutoCodeForEachCached**, equivalent to **AutoCodeForEach** with cached last code values.
* New concept: **SqlDependsOnID**, an alternative for **SqlDependsOn** when one SQL object depends
  on a data structure (table or view), but does not depend on the data structure's properties (other columns).

### Internal improvements

* Bugfix: *FullTextSearch* on a complex query sometimes fails with a "FrameworkException:
  Error while parsing FTS query. Not all search conditions were handled."
* Bugfix: "InvalidCastException" when applying a generic property filter after **FilterBy**.
* Bugfix: Invalid NuGet package reference to *Oracle.ManagedDataAccess*.
* Bugfix: "SqlException: Column name or number of supplied values does not match table definition"
  when inserting a record to an entity that is a named **Polymorphic** implementation with an **AutoCode** property.
* Refactoring the **AutoCode** concept to avoid using the instead-of trigger.
* Removed excess dependencies from polymorphic subtype with named implementation.
* New low-level concept **Initialization**, for injecting initialization code into the Save method.
* The **KeyProperties** concept can now be used inside **ComputedFrom** on a data structure.
  In previous versions it could be used only inside Persisted or inside ComputedFrom on a property (as KeyProperty).

## 1.2.0 (2016-10-19)

### New features

* Allowed execution of data-migration scripts in wrong order, when a new script is ordered before the old one that was already executed.
  In the previous versions, such scripts would be skipped.
  * Set the `DataMigration.SkipScriptsWithWrongOrder` parameter in *Web.config* to `False` to enable this feature, or `True` for old behavior (default).

### Internal improvements

* Removed dependencies to 32-bit dlls; it is no longer required to use 32-bit IIS Application Pool for the Rhetos server.

## 1.1.0 (2016-09-16)

### New features

* New concept: **CreatedBy** writes the current user's ID when saving a new record.
* Generic filter operators `Greater`, `GreaterEqual`, `Less`, `LessEqual` are supported for GUID properties.
  These are available in web APIs such as [REST service](https://github.com/Rhetos/RestGenerator).

### Internal improvements

* **AutoCode** property is no longer **Required**. Saving NULL value will the generate next number.
* **SqlDependsOnIndex** concept is enabled for use in DSL scripts.

## 1.0.0 (2016-08-23)

### Breaking changes

* Modified parameters of `DatabaseExtensionFunctions.FullTextSearch` method.
* Building Rhetos from source requires Visual Studio 2015 on the development environment. If using command-line build, NuGet command-line tool is also required because of Automatic Package Restore.
* For referenced NuGet packages, the lowest compatible package version is deployed, instead of the highest. Visual Studio behaves the same.

### New features

* Enabled localization plugins for end-user messages, such as [I18NFormatter](https://github.com/Rhetos/I18NFormatter).
* Publishing Rhetos NuGet package, for easier development of Rhetos plugins.
* **InvalidData** concept allows programmable error messages by using new concepts **MessageParametersConstant**, **MessageParametersItem** or **MessageFunction**.
* New concept: **SamePropertyValue**, for optimization of inherited row permissions.

### Internal improvements

* Bugfix: Sorting by multiple properties uses only the last one. The bug affected *ReadCommand* used in web APIs such as REST service (*RestGenerator*).
* Bugfix: Reference and Unique constraint errors should include entity and property names.
* Bugfix: Missing encoding info in the JSON error response content type.
* Bugfix: Adding a dependency between existing SQL objects may cause a KeyNotFoundException in DatabaseGenerator.
* Bugfix: Hierarchy path is limited to 70 characters.
* Forcing web service to close SQL transaction *before* sending web response. This is necessary for consistent behavior or consecutive server requests when SQL server uses snapshot isolation.
* `EntityFramework.UseDatabaseNullSemantics` configuration option sets Entity Framework to generate simpler SQL queries.
* Repository's members available in the **Action** code snippet.
* Fixed IIS Express deployment.
* Optimized reading one record by ID.
* New *DeployPackages.exe* switch `/DatabaseOnly`, for improved performance when deploying a farm of Rhetos applications connected to a single database. Multiple instances of the application server can be quickly updated be copying server files, and the database can be quickly upgraded by using the DatabaseOnly switch.
* Full-text search support on Entity Framework.
* Bugfix: The *FullTextSearch* method now works on a **Browse** data structure.
* Bugfix: Targeted .NET Framework for NuGet plugins should be 4.5.1, not 4.0.0.
* Obsolete *SimpleWindowsAuth* package moved to a separate [repository](https://github.com/Rhetos/SimpleWindowsAuth).
* NuGet packages for Rhetos plugins can now be references in standard VS projects (using "lib" instead of "plugins" subfolder, using regular dependencies instead of frameworkAssembly).

## 1.0.0-alpha006 (2015-10-12)

### Breaking changes

* The *DownloadReport* server command no longer requires additional *Read* permissions for all the data sources,
  only the existing *DownloadReport* permission for the report.
* Bugfix: **AutodetectSqlDependencies** applied on one **Module** also creates SQL dependencies on other modules.

### New features

* Automatic authentication cache invalidation when modifying principals, roles and permissions.
  The `AuthorizationCacheExpirationSeconds` option in *Web.config* can now be increased, if Active Directory integration is not used.
* New generic property filters: `EndsWith`, `NotContains`, `DateNotIn`.
  Also available in [REST API](https://github.com/rhetos/restgenerator#filters).

### Internal improvements

* Bugfix: Concurrent inserts to *Common.Principal* may result with unique constraint violation on AspNetUserId, when using *AspNetFormsAuth*.
* Bugfix: **AutoCode** may cause unique constraint violation when READ_COMMITTED_SNAPSHOT is enabled.
* Bugfix: "Invalid column name" error may occur on **Polymorphic** automatic property implementation (missing SQL dependency).

## 1.0.0-alpha004 (2015-08-17)

### Breaking changes

* **Entity Framework** 6.1.3 is used for ORM, instead of **NHibernate**.
  See [Migrating Rhetos applications from NH to EF](https://github.com/Rhetos/Rhetos/wiki/Migrating-Rhetos-applications-from-NHibernate-to-Entity-Framework).

### New features

* Query or load data from repositories using `Query()`, `Query(filter)`, `Load()` or `Load(filter)` methods.
  The `filter` can be a lambda expression (`item => ...`) or an instance of any filter parameter that is supported for this data structure.
* `query.ToSimple()` method, for removing navigation properties when querying a Rhetos data structure from repository.
  For better memory and CPU performance when materializing a LINQ query, instead of `query.ToList()`, use `query.ToSimple().ToList()`.
  If only few properties are used, a better solution would be `query.Select(new { .. properties the will be used .. }).ToList()`.

### Internal improvements

* In the business layer object model (ServerDom) for each data structure there are 2 classes created:
  * A simple class (`SomeModule.SomeEntity`), for working with entity instances in `FilterBy` concept or `Load(filter)` function.
    That class does not contain navigation properties for references to another data structures. It contains simple Guid properties for those references.
  * A queryable class (`Common.Queryable.SomeModule_SomeEntity`), for working with LINQ queries in `ComposableFilterBy` or `ItemFilter` concept, or `Query(filter)` function.
* For reference properties, the scalar Guid property is mapped to the database.
  Previously only navigation properties were available in LINQ queries and in REST filter parameters.
  For example, the client had to use `SomeReference.ID` instead of `SomeReferenceID` in filters; now both options are available and `ReferenceID` is the preferred option.

## 0.9.36 (2015-07-14)

### New features

* New concept: **UserRequired**, for validating input to the Save method.

### Internal improvements

* New *DeployPackages.exe* argument `/ShortTransactions`, for upgrading large databases.
* Improved **Polymorphic**: multiple **Where** allowed, optimized update of *Materialized* extension.
* Bugfix: **Polymorphic** does not update *Materialized* extension for *named* implementations.
* SqlDependsOn* concepts can be used on polymorphic subtype (**Is**) to define dependencies for the generated view.
* Bugfix: Error "DependsOn data structure must be IWritableOrmDataStructure" when using an entity with **History** as a **Polymorphic** subtype.
* DeployPackages is logging more detailed information when refreshing a dependent database object.
* **FilterBy** expression is allowed to use the repository's member methods.
* New internal concept **SuppressSynchronization**, for turning off automatic recompute of an entity with KeepSynchronized.

## 0.9.35 (2015-07-03)

### New features

* New concept: **AutoCodeCached**, an optimized version of **AutoCode** for large tables.
  **AutoCodeCached** stores the latest used code, so it does not need to read the existing records when generating a new code,
  but it requires manual initialization the persisted data at initial deployment or import database records.

### Internal improvements

* Data-migration scripts from a dependent package will be executed *after* the scripts from the package it depends on,
  instead of alphabetically by the module name.
* New internal concept: SqlNotNull.

## 0.9.34 (2015-05-26)

### Internal improvements

* Allowed use of full-text search in LINQ queries through NHibernate extensions. Limited use on Browse data structure: *FullTextSearch* must be used on Base property.
* Bugfix: Using the GUID property associated with a **Reference** property on **Browse** does not work in LINQ queries and generic property filters.

## 0.9.33 (2015-05-12)

### Breaking changes

* Modified error messages for **RowPermissions** read and write access errors.
  Different error messages are used to separate denied access to modify existing data from denied access to apply new data.

### New features

* New concept: **Where**, for filtering **Polymorphic** subtype implementation.

### Internal improvements

* Allowed use of the GUID property associated with **Reference** property, in LINQ queries and generic property filters (web API).
  For example, `Where(item => item.ParentID != null)` is now allowed, along with the old code `Where(item => item.Parent.ID != null)`.
* Performance: **KeepSynchronized** concept sometimes called *Recompute* function twice for same ID.

## 0.9.32 (2015-04-22)

### Internal improvements

* Bugfix: User sometimes gets reduced permissions when using ActiveDirectorySync with non-domain roles.
* Bugfix: CommonAuthorizationProvider log reports more roles than the user has.
* Performance: Use `Security.LookupClientHostname` option to turn off reverse DNS lookup.
  The client hostname can be used for logging in an enterprise environment.
  If the option is turned off in *Web.config*, the client IP address will be used instead of the hostname.

## 0.9.31 (2015-04-16)

### New features

* To automatically add unregistered users to Rhetos users list,
  enable `AuthorizationAddUnregisteredPrincipals` option in *Web.config*.
  This feature can be useful with Windows authentication and *ActiveDirectorySync* Rhetos plugin,
  when configuring Rhetos permissions for domain user groups only.

### Internal improvements

* Minor performance improvements in user authorization.

## 0.9.30 (2015-03-31)

### Internal improvements

* Bugfix: Error "There is no principal with the given username" when using *SimpleWindowsAuth* package.
* Bugfix: Installing a newer package version than specified in the *RhetosPackages.config*.
* Bugfix: Error "An item with the same key has already been added" in NHibernatePersistenceEngine.

## 0.9.29 (2015-03-23)

### Breaking changes

* *SimpleWindowsAuth* package is now obsolete, and should be used only for backward compatibility in legacy applications.
  When removing the package from an existing application, the following changes will occur:
  * Rhetos admin GUI for setting user's permissions is no longer available (`/Resources/Permissions.html`).
  * User names in `Common.Principal` must start with domain name prefix (domainname\username).
  * Domain user groups should be moved from `Common.Principal` to `Common.Role`.
  * Deploy [*ActiveDirectorySync*](ActiveDirectorySync/Readme.md) package to automatically update principal-role membership (`Common.PrincipalHasRole`) for domain users and groups.
  * Each user must be entered in `Common.Principal`, *SimpleWindowsAuth* allowed entering only user groups.
    For backward compatibility enable `AuthorizationAddUnregisteredPrincipals` option in *Web.config* on Rhetos v0.9.31 or later.

### New features

* Roles (`Common.Role`) can be used for grouping permissions and users. A role can also inherit permissions or include users from other roles.
* New package *ActiveDirectorySync*: for synchronizing Rhetos principal-role membership with Active Directory.
  See [ActiveDirectorySync\Readme.md](ActiveDirectorySync/Readme.md) for more info.
* Caching permissions, configurable by `AuthorizationCacheExpirationSeconds` value in *Web.config*.

### Internal improvements

* User authorization (permissions) and Windows authentication is now implemented in *CommonConcepts* package, instead of *SimpleWindowsAuth* and *AspNetFormsAuth*.
* New concept: **Query** with parameter, for parametrized reading data using IQueryable.
* New low-level concepts: **BeforeQuery** with parameter and **BeforeAction**, for injecting code in business object model.
* New concept: **DefaultLoadFilter**, for limiting automatic computation on a subset of rows on **KeepSynchronized**.
* Bugfix: `Like` function on string throws exception "The call is ambiguous...". Old implementation of the function is renamed to `SqlLike`.
* Bugfix: SQL error when using **Polymorphic** for automatic implementation of **Reference** property.
* Bugfix: Application-side verification of **Unique** constraint does not work (it can be used on LegacyEntity).
* Bugfix: Creating an index that is both **Unique** and **Clustered** fails with an SQL syntax error.
* Bugfix: NHibernate proxy error when using **KeepSynchronized** on entity with **Detail** entities.

## 0.9.28 (2015-03-06)

### Breaking changes

* Upgraded from .NET Framework 4.0 to .NET Framework 4.5.1.
* Upgrading to the latest version of NHibernate, NLog, DotNetZip and Newtonsoft.Json.

## 0.9.27 (2015-03-05)

### New features

* **Polymorphic** now allows specific SQL query implementation (using **SqlImplementation**), and also **SqlQueryable** as a subtype implementation.

### Internal improvements

* Using [Semantic Versioning](http://semver.org/spec/v2.0.0.html) in Rhetos build and dependency analysis.
* Added "Installed packages" list to the Rhetos homepage.
* Bugfix: Missing administration GUI for SimpleWindowsAuth.
* Bugfix: Missing explicit namespace in Rhetos homepage.

## 0.9.26 (2015-02-26)

### Breaking changes

* Removed obsolete class `Rhetos.Utilities.ResourcesFolder` (use `Paths.ResourcesFolder` instead).

### New features

* New low-level BL object model concepts: **SaveMethod**, **OnSaveUpdate**, **OnSaveValidate**, **LoadOldItems**.
  These are concepts for injecting business logic in the entity's repository.
  They should not be used directly in DSL scripts if the business requirements can be implemented
  by using higher level concepts such as **Persisted**, **Lock**, **InvalidData** (DenySave), **DenyUserEdit**, etc.
* `DeployPackages.exe /IgnoreDependencies` switch, allows installing incompatible package versions.

### Internal improvements

* `ExecutionContext` class now exposes `GenericRepository` constructor with both interface and entity name.
  This is useful for writing type-safe code that uses entity through an interface, without referencing the generated object model.
  Example: `executionContext.GenericRepository<IDeactivatable>("Common.Claim").Query(item => item.Active == false)`.
* Added "Rhetos." prefix to standard Rhetos packages. The generated `Resources\<PackageName>` subfolder for each package is named without the prefix, to keep backward compatibility.
* Checking package dependency on Rhetos framework version using `frameworkAssembly` element in *.nuspec* file.
* Bugfix: Case insensitive and *null* comparison in *GenericFilter* sometimes do not work (REST API and ReadCommand filters).
* Bugfix: *Read* claim is generated for some non-readable data structures (**Action**, for example).
* Bugfix: Logging Rhetos server commands and results works only for SOAP web API, not for REST (see `TraceCommandsXml` in *Web.config*).
* Client application may ignore properties marked with **DenyUserEdit**, the server will accept *null* value from client and keep the old value unchanged.
* Logging slow SQL queries from *ISqlExecuter*.
* Logging file-locking issues on deployment.

## 0.9.25 (2015-02-19)

### Breaking changes

* `ExtractPackages.exe` tool is deleted. Its functionality is now part of `DeployPackages.exe`.
* `CreatePackage.exe` tool is deleted. New Rhetos packages should be packed by [NuGet](https://www.nuget.org/).
* `DeployPackages.exe` reads the package list from *RhetosPackages.config* and *RhetosPackageSources.config*.
  Empty prototypes of those files are created on first deploy.
* `DeployPackages.exe` will no longer read source files from `DslScripts` and `DataMigration` subfolders in the Rhetos server application (e.g. `RhetosServer\DslScripts`).
  * Please **backup** all Rhetos server folders and **delete** obsolete subfolders `DslScripts` and `DataMigration`.
* **FilterByReferenced** will yield syntax error if the referenced filter name does not match exactly.
  In previous versions, a different filter name was allowed by optional namespace.

### New features

* `DeployPackages.exe` can now fully deploy Rhetos packages to Rhetos server, without relying on other tools to extract or copy package's content into a Rhetos server.
  The following types of packages are supported:
    1. NuGet packages
    2. Unpacked source folders, for development environment
    3. Legacy zip packages, for backward compatibility
* `DeployPackages.exe` handles package dependencies (see [NuGet versioning](https://docs.nuget.org/create/versioning)).
  It will verify if package versions are compatible and automatically download referenced packages.
* **SqlObject** can be created without transaction.
  This is necessary for deploying SQL objects that cannot be created inside a transaction (*Full-text search index* on MS SQL Server, e.g).
* New concepts: **ComposableFilterByReferenced** and **ItemFilterReferenced**,
  for inheriting filters from referenced data structure.
* New concept: **InvalidData**, to replace obsolete **DenySave** (same syntax).
* `DeployPackages.exe` will pause on error, unless `/NoPause` command-line argument is used.

### Internal improvements

* *IValidatedConcept* interface allows improved performance in semantic validation. Old interface *IValidationConcept* is now obsolete.
* Detailed logging of permission analysis in *SimpleWindowsAuthorizationProvider*.
* `DeployPackages.exe` reports use of obsolete concepts.
* `AdminSetup.exe` in *AspNetFormsAuth* also creates admin user and permissions, to make sure the admin account is property set up.
* Better *EndOfFile* error handling in DSL parser.

## 0.9.24 (2015-02-04)

### Breaking changes

* Using new version of Autofac (v3.5.2). Since Autofac dlls are signed, all plugin packages that use Autofac must be rebuilt with the new version.

### New features

* New concept: **LockExcept**, for locking entity without locking the specified properties.

### Internal improvements

* `DeployPackages.exe` uses a logger instead of `Console.WriteLine()` directly. The logger can be configured in `DeployPackages.exe.config`.
* Deployment performance improvements.

## 0.9.23 (2015-01-26)

### Breaking changes

* Internal server error information is removed from server responses (a configuration option will be added in future).

### New features

* New row permissions concepts: **Allow** and **Deny**, for combined read and write rules.
* Simplified row permission rules format: a single function that returns the expression.

### Internal improvements

* Bugfix: Deploying AspNetFormsAuth after SimpleWindowsAuth fails on IX_Permission_Role_Claim.
* Bugfix: User authorization fails when using IProcessingEngine (executing server commands) in unit tests and in LinqPad scripts.

## 0.9.22 (2015-01-13)

### Breaking changes

* **Detail** reference is **SystemRequired**.

### New features

* Added [Forgot my password](https://github.com/Rhetos/AspNetFormsAuth#forgot-password) feature to *AspNetFormsAuth*.
  Sending the password reset token to the user (by SMS or email, e.g.) is to be implemented as a separate plugin
  (for example see [SimpleSPRTEmail](https://github.com/Rhetos/SimpleSPRTEmail)).
* New RowPermissions concepts: **AllowWrite** and **DenyWrite**.
* New RowPermissions concept: **InheritFromBase**.
* New RowPermissions concept: **AutoInheritRowPermissions**.

### Internal improvements

* Minor performance optimizations for some macro concepts.
* Bugfix: InitializationConcept was not registered as a regular concept.

## 0.9.21 (2014-12-18)

### Internal improvements

* Bugfix: Filter by ID cannot be combined with queryable filters. Some queries may throw NHibernate error, such as using row permissions on filters browse data structure.
* Bugfix: **SqlIndexMultiple** sometimes did not use the given ordering of properties.
* Bugfix: *SqlCommandTimeout* config parameter does not apply to NHibernate queries.
* Bugfix: **AutoCode** fails with unique index constraint error on concurrent inserts.
* Bugfix: Duplicate unique value error in *IX_Permission_Principal_Claim* on deployment, when migrating roles from *AspNetFormsAuth* to *SimpleWindowsAuth* package.
* New concept: **Clustered**, for SQL indexes.
* Minor performance optimizations.

## 0.9.20 (2014-12-09)

### Breaking changes

* Filter `Common.RowPermissionsAllowedItems` renamed to `Common.RowPermissionsReadItems`.

### New features

* New concepts: RowPermissions **AllowRead** and **DenyRead**, helpers for row permissions that allows combining multiple rules and inheriting rules from one entity to another.

### Internal improvements

* Improved performance of *DeployPackages.exe* (mostly DSL parser).
* Automatically updating persisted **KeepSynchronized** data on first deployment. The update may be avoided for a specific computation by setting `Context='NORECOMPUTE'` in *Common.KeepSynchronizedMetadata* table.
* Bugfix: **SqlDependsOn** to an entity without properties does not work.
* Bugfix: *ArgumentNullException* thrown on some client request.
* Bugfixes: Row permissions not supported on **Browse**. Row permissions not used on empty filter requests.
* New interface *IConceptMacro*, for implementing macro concepts with better performance.
* Added Query function to *GenericRepository*, for simpler use in application code.

## 0.9.19 (2014-11-12)

### New features

* New concept: **Polymorphic**. It allows multiple entities to implement same interface.
  Polymorphic data structure is readable, it returns the union of all implementations.
  A reference to a polymorphic entity will validate foreign key constraint on all data changes.
* New concept: **RowPermissions** for programmable constraints on records that a client is allowed to read.
  Can be used explicitly by filter parameter: *Common.RowPermissionsAllowedItems*.
* New concepts: **ApplyFilterOnClientRead** and **ApplyOnClientRead**, for filters that are automatically added to each client request.

### Internal improvements

* New action for custom log entries: *Common.AddToLog*.
* Bugfix: Value-type filter without a given parameter value would fail even if the value is not used in the filter.
* Using *ConceptMetadata* for sharing concept implementation info (SQL column name and type, e.g.).

## 0.9.18 (2014-10-01)

### New features

* **AutoCode** for **Integer** properties.
* **AutoCode** allows client to define minimal number length by multiple "+" at the end of the format string.
* New persistence control mode: **ComputeForNewItems**.
* **History** allows *ActiveSince* in future. "Current" entry is now simply the latest one (may have future date).
* Enabled use of **UseExecutionContext** on **ComposableFilterBy**.

### Internal improvements

* Added friendly error messages for authentication and permission problems while running Rhetos under IISExpress or when server process lacks MS SQL permissions.
* New exception type: *ClientException*, to separate client software errors, such as invalid request format, from denied user actions (UserException).
* Async logging for trace log.
* Bugfix: Allow multiple **AutoCode** on same Entity.
* Bugfix: **DenyUserEdit** with **CreationTime** on same property fails on save.
* Bugfix: Reference to an **SqlQueryable** causes SQL error on deployment.
* Bugfix: *ApplyPackages.bat* fails on space in package's folder name.
* Bugfix: **AutodetectSqlDependencies** and other SqlDependsOn* concepts should not be case sensitive.

## 0.9.17 (2014-05-21)

### Breaking changes

* Testing API: RhetosTestContainer.InitializeRhetosServerRootPath function removed, use the constructor argument instead.

### New features

* New concepts: **KeyProperty** and **KeyProperties**, for **ComputedFrom** to control when to update an item or to delete old item and insert a new one.
  Backward compatible with previous ComputedFrom behavior where key property is assumed to be "ID".
* AspNetFormsAuth: Token expiration parameter for *GeneratePasswordResetToken*.
* `/Debug` option for DeployPackages.exe to generate ServerDom.dll without optimizations.

### Internal improvements

* Bugfix: Reading *top n* or *count* from Browse data structure loads all rows from database.
* Bugfix: TargetInvocationException instead of UserException was reported to the client.
* More minor bugfixes and error handling improvements.

## 0.9.16 (2014-04-18)

### Internal improvements

* New core DSL concept **InitializationConcept**, for singleton DSL implementation plugins.
* Bugfix: LinqPad script cannot detect Rhetos server's folder it the folder's name is not "Rhetos".

## 0.9.15 (2014-04-07)

### New features

* Implemented `GenericRepository` class, a helper for server-side type-safe access to entity's repository (using interface the entity implements) without referencing the generated business layer object model.
* *Generic filter* is extended to allow multiple predefined filters (along with property filters).
* New server command: *ReadCommand* is a replacement for *QueryDataSourceCommand*.
  * Improvements: ordering by multiple properties, more paging control with Top and Skip, reading records with paging without getting total count (this was a performance issue) and reading record count without reading records.
  * *QueryDataSourceCommand* is obsolete, but still available for backward compatibility.

### Internal improvements

* Deactivating obsolete claims and permissions, instead of deleting them.
* Unit tests and LinqPad scripts now use full Rhetos server context (RhetosTestContainer class). This allows using reports, authentication manager and other server components.
* Added GenericRepositories in ExecutionContext.
* AspNetFormsAuth: Implemented GeneratePasswordResetToken and ResetPassword web methods. Automatic user log in after ResetPassword.
* Minor improvement in DeployPackages.exe performance (cached plugins scanning).
* Bugfix: AutocodeForEach on a Reference with short syntax causes error "Group property type 'SimpleReferencePropertyInfo' is not supported".
* Bugfix: NHibernate.Cfg.Configuration.LinqToHqlGeneratorsRegistry cannot be used in plugins.
* Bugfix: Missing dll dependencies for AspNetFormsAuth.
* Bugfix: Deploying AspNetFormsAuth to an empty database causes SQL connection timeout.

## 0.9.14 (2014-02-26)

### New features

* New package: **AspNetFormsAuth**.
  It provides an implementation of ASP.NET forms authentication to Rhetos server applications.
  See [AspNetFormsAuth\Readme.md](AspNetFormsAuth/Readme.md) for more info on features and installation.
* New package: **SimpleWindowsAuth**.
  It contains the existing Windows authentication and authorization subsystem, extracted from Rhetos core framework and CommonConcepts package.
* New concept: **RegisteredImplementation**,
  for exposing repositories of an entity that implements a given interface.
  It helps to keep algorithm implementations out of DSL scripts by providing statically-typed querying and saving of generated object model entities without referencing the generated assembly.

### Internal improvements

* Bugfix: Absolute URI (localhost Rhetos server) removed from *Web.config*.
* Bugfix: **DenyUserEdit** and **SystemRequired** concepts denied saving valid data entries when using automatic value initialization.
* Bugfix: `SetupRhetosServer.bat` sometimes reported incorrect error "IIS Express is not installed".
* Performance: First call of the *DownloadReport* server command sometimes takes more time.
* New plugin type: *IHomePageSnippet*, for adding development and administration content to Rhetos homepage.
* New plugin type: *ICommandObserver*, for extending server's command handling.

## 0.9.13 (2013-11-28)

### Internal improvements

* Bugfix: **RegExMatch** did not escape C# special characters when generating object model. Matching values are now tested for exact match, not substring match.
* Added custom error message property to **RegExMatch**.
* Bugfix: Without a network access to the Active Directory server, every command throws an exception (even with BuiltinAdminOverride).
* Bugfix: GenericFilter throws an exception when filtering for null reference value.
* Bugfix: **Decimal** precision was limited to 5 instead of 10 for decimal(28,10).
* Bugfix: Editing an entity with **History** does not create a new history record when using web API.
* Bugfix: Escaping special characters in C# string in **HierarchyWithPathInfo**, **MinValueInfo** and **MaxValueInfo**.
* Bugfix: Renaming a claim resource by changing letter case would cause an exception at DeployPackages.
* Bugfix: Removed a reference from core framework (Rhetos.Security) to CommonConcepts package.

## 0.9.12 (2013-11-06)

### New features

* Tracking of related items for *Common.Log* allows searching for all logged events of a given entity, including events of its detail entities and extensions.

### Internal improvements

* Bugfix: Saving an entity with Lock or History concept sometimes resulted with an NHibernate.LazyInitializationException error.
* Bugfix: Updating ActiveItem in history should not create new entry in Changes table.
* Bugfix: DeployPackages sometimes uses old migration data in DataMigrationRestore.
* Bugfix: FK_AppliedConceptDependsOn_DependsOn error on DeployPackages.
* NHibernate updated to version 3.3.3 SP1 (HqlTreeBuilder.Constant `IList<T>` issue).
* Improved ORM DateTime precision from seconds to milliseconds.

## 0.9.11 (2013-09-25)

### New features

* New concept: **AutodetectSqlDependencies** automatically detects and generates dependencies (**SqlDependsOn**) for SqlQueryable, SqlView, SqlFunction, SqlProcedure, SqlTrigger and LegacyEntity view. It may be applied to any of those objects or to a whole module.

### Internal improvements

* Improved performance of `DeployPackaged.exe`. Optimized update of concepts' metadata in database generator.
* Bugfix: **DenySave** that uses **SqlQueryable** sometimes caused an error "Could not initialize proxy" on save.
* Bugfix: Web query that combines **ItemFilter** or **ComposableFilter** with *GenericFilter* sometimes caused case insensitive string filtering or NullReferenceException. Filter was executed in C# instead of the SQL.
* Bugfix: Rhetos REST service bindings were not loaded from *Web.config*.
* Bugfix: On some systems the PUT method on Rhetos REST service caused HTTP error 405. Removed WebDAVModule.
* Bugfix: InvalidCastException (OracleDataReader.GetInt32) on some systems while upgrading database.
* Improved error handling in build batch scripts. Use `/NOPAUSE` parameter for automated builds to avoid pause on error.

## 0.9.10 (2013-09-12)

### Breaking changes

* REST interface (DomainService.svc) is moved from Rhetos core to a separate repository: *LegacyRestGenerator*. A faster version of REST interface is implemented in *RestGenerator* repository.
* Modified implementation of **History** concept: Generated `_FullHistory` data structure is renamed to `_History`, old `_History` to `_Changes` and `_History_ActiveUntil` to `_ChangesActiveUntil`.

### New features

* When using **SqlDependsOn** for property or entity, the concept will automatically add s dependency to the property's unique index if one exists.

### Internal improvements

* Bugfix: Concept info property 'LegacyPropertySimpleInfo.LegacyEntityWithAutoCreatedView' is not initialized.
* Bugfix: ArgumentNullException when loading Common.Claim or Common.Principal.
* Bugfix: Generated dlls moved to bin\Generated, to avoid locking during execution of DeployPackages.exe.

## 0.9.9 (2013-09-04)

### New features

* Writeable **EntityHistory**. FullHistory data structure now allows insert/update/delete commands by automatically updating history entries and current entry.

### Internal improvements

* New concept: **Write** allows creating a Save function and corresponding WEB methods for data structure that is not writeable by default.
* Bugfix: Trace log should be disabled by default for better performance.
* Bugfix: DeployPackages did not generate claims for new entities.
* Bugfix: DeployPackages.exe and CleanupOldData.exe could not remove old tables and columns whose names are no longer supported by Rhetos (identifiers that need to be quoted).

## 0.9.8 (2013-08-30)

### Breaking changes

* The C# code snippet in **QueryableExtension** must assign both ID and Base property of the created instance. Previously it was enough to assign only the Base property in certain situations.
* Uninitialized **ShortString** property has null value, previously it was empty string. Uninitialized **ID** property is Guid.Empty, previously it was Guid.NewGuid(). Note that when saving an entity, the ID value will still be automatically generated if it was not set in advance.
* Modified interface of *Tag* class (used by code generator plugins).

### New features

* New concept: **SystemRequired**, for a property that must be computed by the server. Note that the existing **Required** concept should be used to enforce a business rule when a user must enter the property's value.
* New concept: **DenyUserEdit**, for a property that may only be changed by the server, not by a client Save request. It may also be applied to an entity with hardcoded system data.

### Internal improvements

* Helper classes *CsTag*, *SqlTag* and *XmlTag* provided a simplified creation of code tags (for code generator plugins).
* Bugfix: **LongString** and **Binary** properties were limited to 8000 bytes.
* DSL packages may contain custom web service registration.
* Implicit transactions with NHibernatePersistenceTransaction allow late query evaluation that is required for OData service.
* Removed *TypeFactory*, *AspectFactory*, *InterceptorFactory* and *DynamicProxyFactory*. TypeFactory was a wrapper around Autofac, but it did not provide a useful abstraction layer. Other components were planned for AOP, but they were not used in practice. AOP principles are already fully supported by code generators for the final application. These features were not used for internal framework components.
* More flexible plugins registration using *PluginsUtility* and *PluginsContainer*.
* Removed backing fields for properties in server object model.
* **FullHistory** data structure implementation changed to SqlQueryable instead of generated view and legacy entity, so that other concepts may use the SqlQueryable with **SqlDependsOn**.
* **Computed** data structure is now available through REST interface.
* Better handling of null values and derived property types in **MinLength**, **MinValue**, **MaxLength** and **MaxValue**.
* Enabled use of **UseExecutionContext** concept on **Action**.
* Bugfix: Recursive updates with KeepSynchronized could cause infinite loop even if there is nothing to update.
* **CreationTime** implementation moved from database to object model (data import should not change the migrated creation time even if the value is not specified).

## 0.9.7 (2013-08-02)

### New features

* New concept: **Deactivatable** allows records to be deactivated instead of deleted.
* Improved **History** concept: *ActiveUntil* property computed for each history record. FullHistory available through REST interface. Better validations.

### Internal improvements

* Added ability to extend Rhetos with custom file generators.
* Removed end-of-line normalization of git repository.

## 0.9.6 (2013-07-12)

### Breaking changes

* REST error result was previously a JSON string. Now the result is an object with string properties *UserMessage* and *SystemMessage*. UserMessage should be reported to the end user. SystemMessage contains additional system information, such as entity or property that caused an error.
* REST method for inserting an entity record (POST) previously returned the generated ID as a string. Now the command returns an object with GUID property named *ID*.

### New features

* New concepts for simplified validations: **MaxLength**, **MinLength**, **MaxValue**, **MinValue**, **RegExMatch**, **Range**, **IntegerRange**, **DateRange** and **DateTimeRange**.
* New version of concepts **DenySave**, **LockItems** and **LockProperty** with additional reference to the property that is being validated. That property will be reported to the client in case of an error during Save.

### Internal improvements

* New concept: **ComputedFrom** is a more flexible version of **Persisted**. It allows a property-level recomputing instead of entity-level. It is intended to be used as an internal concept for building simpler macro concepts.
* Better handling of plugins: allowed non-default constructors for all plugins, simplified plugin registration and retrieval.
* Bugfix: Set default git repository configuration to use CRLF for end-of-line.
* Bugfix: Using AllPropertiesFrom to copy properties with an SqlIndex to a data structure that does not support SqlIndex will throw an error.
* Bugfix: NHibernate mapping for properties did not apply to derivations of the existing property types.
* An IMacroConcept may create an IAlternativeInitializationConcept without setting non-parsable properties.

## 0.9.5 (2013-06-28)

### Breaking changes

* Changed SOAP interface of the server: *ServerProcessingResult* property *enum State* changed to *bool Success*.

### New features

* Rhetos is an open source software under AGPL licence!
* New concept: **SimpleReferencePropertyInfo** simplifies writing a DSL script when a reference property name is the same as the referenced entity.
* Improved DSL parser: IAlternativeInitializationConcept allows a DSL concept to simplify its syntax or its internal design when depending on other automatically created concepts.

### Internal improvements

* Bugfix: Added buffering in FilterByReferenced concept.
* Refactoring of unit tests: private accessors are no longer used.

## 0.9.4 (2013-06-18)

### Breaking changes

* Concept **Snowflake** renamed to **Browse**.

### New features

* New concept: **Take**, for easier modelling of the Snowflake.
* Improved error handling on Snowflake.

## 0.9.3 (2013-06-13)

### Internal improvements

* Bugfix: Filtering by ID (Guid[]) could not load more than 1000 items.
* Bugfix: Local admin override did not work correctly while UAC is enabled and VisualStudio is not started as Administrator.
* Bugfix: Fixed Rhetos project dependencies needed for GetServerFiles.bat.
* Bugfix: Snowflake returned 0 records when used on a QueryableExtension.
* Bugfix: Filter by DateTime did not work on an Entity with partial History (subset of it's properties).
* Bugfix: Creating _FullHistory view sometimes failed because of undefined dependencies.

## 0.9.2 (2013-06-13)

### Breaking changes

* Renamed concept **All** to **AllProperties** (used with **Logging**).

### New features

* New concept: **History**, for automatic management of older versions of a record (some or all properties).
  It also provides a functions for retrieving the record's state at a given time.
* **Detail** concept automatically includes SqlIndex.
* New concepts for copying properties from another data structure: **PropertyFrom** and **AllPropertiesWithCascadeDeleteFrom**.
  Existing **AllPropertiesFrom** modified to not include cloning CascadeDelete concepts.

### Internal improvements

* Modifies DSL parser to allow disambiguation of similar concepts with the same name (AllProperties, e.g.) depending on the context (Logging, Persisted, History, e.g.).

## 0.9.1 (2013-06-05)

### Breaking changes

* Removed obsolete SOAP command *VerifyAuthorizationCommand*.

### New features

* New concept: **ModificationTimeOf**. It automatically updates the modification time of the given property.
* New concept: **CreationTime**. It automatically sets the record's creation time.
* New concept: **SqlObject**, for clean creation and deletion of any type for SQL object (such as an SQL Server job) through a DSL script.
  Use SqlDependsOn to set dependencies to other entities or properties.
  Use SqlDependsOnSqlObject to set dependencies of other DSL objects to an SqlObject.
* New concept: **ComputeForNewBaseItemsWithFilterInfo**, similar to KeepSynchronizedWithFilteredSaveInfo.

### Internal improvements

* Better performance of permission checking.
* Bugfix: Installation package did not contain Global.asax.
* Bugfix: KeepSynchronizedInfo sometimes caused redundant updates for new items.
