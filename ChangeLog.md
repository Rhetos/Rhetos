0.9.14 (2014-02-26)
-------------------

New features:

* New package: **AspNetFormsAuth**.
  It provides an implementation of ASP.NET forms authentication to Rhetos server applications.
  See [AspNetFormsAuth\Readme.md](AspNetFormsAuth\Readme.md) for more info on features and installation.
* New package: **SimpleWindowsAuth**.
  It contains the existing Windows authentication and authorization subsystem, extracted from Rhetos core framework and CommonConcepts package.
* New concept: **RegisteredImplementation**,
  for exposing repositories of an entity that implements a given interface.
  It helps to keep algorithm implementations out of DSL scripts by providing statically-typed querying and saving of generated object model entities without referencing the generated assembly.

Internal improvements:

* Bugfix: Absolute URI (localhost Rhetos server) removed from *Web.config*.
* Bugfix: **DenyUserEdit** and **SystemRequired** concepts denied saving valid data entries when using automatic value initialization.
* Bugfix: `SetupRhetosServer.bat` sometimes reported incorrect error "IIS Express is not installed".
* Performance: First call of the *DownloadReport* server command sometime takes more time (building DslModel instance).
* New plugin type: *IHomePageSnippet*, for adding development and administration content to Rhetos homepage.
* New plugin type: *ICommandObserver*, for extending server's command handling. 

0.9.13 (2013-11-28)
-------------------

Internal improvements:

* Bugfix: **RegExMatch** did not escape C# special characters when generating object model. Matching values are now tested for exact match, not substring match.
* Added custom error message property to **RegExMatch**.
* Bugfix: Without a network access to the Active Directory server, every command throws an exception (even with BuiltinAdminOverride).
* Bugfix: GenericFilter throws an exception when filtering for null reference value.
* Bugfix: **Decimal** precision was limited to 5 instead of 10 for decimal(28,10).
* Bugfix: Editing an entity with **History** does not create a new history record when using web API.
* Bugfix: Escaping special characters in C# string in **HierarchyWithPathInfo**, **MinValueInfo** and **MaxValueInfo**.
* Bugfix: Renaming a claim resource by changing letter case would cause an exception at DeployPackages.
* Bugfix: Removed a reference from core framework (Rhetos.Security) to CommonConcepts package. 

0.9.12 (2013-11-06)
-------------------

New features:

* Tracking of related items for *Common.Log* allows searching for all logged events of a given entity, including events of its detail entities and extensions.
 
Internal improvements:

* Bugfix: Saving an entity with Lock or History concept sometimes resulted with an NHibernate.LazyInitializationException error.
* Bugfix: Updating ActiveItem in history should not create new entry in Changes table.
* Bugfix: DeployPackages sometimes uses old migration data in DataMigrationRestore.
* Bugfix: FK_AppliedConceptDependsOn_DependsOn error on DeployPackages.
* NHibernate updated to version 3.3.3 SP1 (HqlTreeBuilder.Constant IList<X> issue).
* Improved ORM DateTime precision from seconds to milliseconds.


0.9.11 (2013-09-25)
-------------------

New features:

* New concept: **AutodetectSqlDependencies** automatically detects and generates dependencies (**SqlDependsOn**) for SqlQueryable, SqlView, SqlFunction, SqlProcedure, SqlTrigger and LegacyEntity view. It may be applied to any of those objects or to a whole module.

Internal improvements:

* Improved performance of `DeployPackaged.exe`. Optimized update of concepts' metadata in database generator.
* Bugfix: **DenySave** that uses **SqlQueryable** sometimes caused an error "Could not initialize proxy" on save.
* Bugfix: Web query that combines **ItemFilter** or **ComposableFilter** with *GenericFilter* somtimes caused case insensitive string filtering or NullReferenceException. Filter was executed in C# instead of the SQL.
* Bugfix: Rhetos REST service bindings were not loaded from `web.config`.
* Bugfix: On some systems the PUT method on Rhetos REST service caused HTTP error 405. Removed WebDAVModule.
* Bugfix: InvalidCastException (OracleDataReader.GetInt32) on some systems while upgrading database. 
* Improved error handling in build batch scripts. Use `/NOPAUSE` parameter for automated builds to avoid pause on error.

0.9.10 (2013-09-12)
-------------------

Breaking changes:

* REST interface (DomainService.svc) is moved from Rhetos core to a separate repository: *LegacyRestGenerator*. A faster version of REST interface is implemented in *RestGenerator* repository.
* Modified implementation of **History** concept: Generated `_FullHistory` data structure is renamed to `_History`, old `_History` to `_Changes` and `_History_ActiveUntil` to `_ChangesActiveUntil`.
 
New features:

* When using **SqlDependsOn** for property or entity, the concept will automatically add s dependency to the property's unique index if one exists.  

Internal improvements:

* Bugfix: Concept info property 'LegacyPropertySimpleInfo.LegacyEntityWithAutoCreatedView' is not initialized.
* Bugfix: ArgumentNullException when loading Common.Claim or Common.Principal.
* Bugfix: Genetared dlls moved to bin\Generated, to avoid locking during exection of DeployPackages.exe.

0.9.9 (2013-09-04)
------------------

New features:

* Writeable **EntityHistory**. FullHistory data structure now allows insert/update/delete commands by automatically updating history entries and current entry.    
 
Internal improvements:

* New concept: **Write** allows creating a Save function and corresponding WEB methods for data structure that is not writeable by default. 
* Bugfix: Trace log should be disabled by default for better performance.
* Bugfix: DeployPackages did not generate claims for new entities. 
* Bugfix: DeployPackages.exe and CleanupOldData.exe could not remove old tables and columns whose names are no longer supported by Rhetos (identifiers that need to be quoted).

0.9.8 (2013-08-30)
------------------

Breaking changes:

* The C# code snippet in **QueryableExtension** must assign both ID and Base property of the created instance. Previously it was enough to assign only the Base property in certain situations.
* Uninitialized **ShortString** property has null value, previously it was empty string. Uninitialized **ID** property is Guid.Empty, previously it was Guid.NewGuid(). Note that when saving an entity, the ID value will still be automatically generated if it was not set in advance.
* Modified interface of *Tag* class (used by code generator plugins).

New features:

* New concept: **SystemRequired**, for a property that must be computed by the server. Note that the existing **Required** concept should be used to enforce a business rule when a user must enter the property's value. 
* New concept: **DenyUserEdit**, for a property that may only be changed by the server, not by a client Save request. It may also be applyed to an entity with hardcoded system data.

Internal improvements:

* Helper classes *CsTag*, *SqlTag* and *XmlTag* provied a simplifyed creation of code tags (for code generator plugins).
* Bugfix: **LongString** and **Binary** properties were limited to 8000 bytes.
* DSL packages may contain custom web service registration.
* Implicit transactions with NHibernatePeristenceTransaction allow late query evaluation that is required for OData service.
* Removed *TypeFactory*, *AspectFactory*, *InterceptorFactory* and *DynamicProxyFactory*. TypeFactory was a wrapper around Autofac, but it did not provide a useful abstraction layer. Other components were planned for AOP, but they were not used in practice. AOP principles are already fully supported by code generators for the final application. These features were not used for internal framework compoments.
* More flexible plugins registration using *PluginsUtility* and *PluginsContainer*.
* Removed backing fields for properties in server object model.
* **FullHistory** data structure implementation changed to SqlQueryable instead of generated view and legacy entity, so that other concepts may use the SqlQueryable with **SqlDependsOn**.
* **Computed** data structure is now available through REST interface.
* Better handling of null values and derived property types in **MinLength**, **MinValue**, **MaxLength** and **MaxValue**.
* Enabled use of **UseExecutionContext** concept on **Action**.
* Bugfix: Recursive updates with KeepSynchronized could cause infinite loop even if there is nothing to update.
* **CreationTime** implementation moved from database to object model (data import should not change the migrated creation time even if the value is not specified).
 
0.9.7 (2013-08-02)
------------------

New features:

* New concept: **Deactivatable** allows records to be deactivated instead of deleted.
* Improved **History** concept: *ActiveUntil* property computed for each history record. FullHistory available through REST interface. Better validations.

Internal improvements:

* Added ability to extend Rhetos with custom file generators.
* Removed end-of-line normalization of git repository.

0.9.6 (2013-07-12)
------------------

Breaking changes:

* REST error result was previously a JSON string. Now the result is an object with string properties *UserMessage* and *SystemMessage*. UserMessage should be reported to the end user. SystemMessage contains additional system information, such as entity or property that caused an error.
* REST method for inserting an entity record (POST) previously returned the generated ID as a string. Now the command returns an object with GUID property named *ID*. 

New features:

* New concepts for simplified validations: **MaxLength**, **MinLength**, **MaxValue**, **MinValue**, **RegExMatch**, **Range**, **IntegerRange**, **DateRange** and **DateTimeRange**.
* New version of concepts **DenySave**, **LockItems** and **LockProperty** with additional reference to the property that is being validated. That property will be reported to the client in case of an error during Save. 

Internal improvements:

* New concept: **ComputedFrom** is a more flexible version of **Persisted**. It allows a property-level recomputing instead of entity-level. It is intended to be used as an internal concept for building simpler macro concepts.
* Better handling of plugins: allowed non-default constructors for all plugins, simplified plugin registration and retrieval. 
* Bugfix: Set default git repository configuration to use CRLF for end-of-line. 
* Bugfix: Using AllPropertiesFrom to copy properties with an SqlIndex to a data structure that does not support SqlIndex will throw an error.
* Bugfix: NHibernate mapping for properties did not apply to derivations of the existing property types.
* An IMacroConcept may create an IAlternativeInitializationConcept without setting non-parsable properties.

0.9.5 (2013-06-28)
------------------

Breaking changes:

* Changed SOAP interface of the server: *ServerProcessingResult* property *enum State* changed to *bool Success*.

New features:

* Rhetos is an open source software under AGPL licence!
* New concept: **SimpleReferencePropertyInfo** simplifies writing a DSL script when a reference propety name is the same as the referenced entity.
* Improved DSL parser: IAlternativeInitializationConcept allows a DSL concept to simplify its syntax or its internal design when depending on other automatically created concepts.

Internal improvements:

* Bugfix: Added buffering in FilterByReferenced concept.
* Refactoring of unit tests: private accessors are no longer used.

0.9.4 (2013-06-18)
------------------

Breaking changes:

* Concept **Snowflake** renamed to **Browse**.

New features:

* New concept: **Take**, for easier modelling of the Snowflake.
* Improved error handling on Snowflake.

0.9.3 (2013-06-13)
------------------

Internal improvements:

* Bugfix: Filtering by ID (Guid[]) could not load more than 1000 items.
* Bugfix: Local admin override did not work correctly while UAC is enabled and VisualStudio is not started as Administrator.
* Bugfix: Fixed Rhetos project dependencies needed for GetServerFiles.bat.
* Bugfix: Snowflake returned 0 records when used on a QueryableExtension.
* Bugfix: Filter by DateTime did not work on an Entity with partial History (subset of it's properties).
* Bugfix: Creating _FullHistory view sometimes failed because of undefined dependencies.

0.9.2 (2013-06-13)
------------------

Breaking changes:

* Renamed concept **All** to **AllProperties** (used with **Logging**).

New features:

* New concept: **History**, for automatic management of older versions of a record (some or all properties).
  It also provides a functions for retrieving the record's state at a given time.
* **Detail** concept automatically includes SqlIndex.
* New concepts for copying properties from another data stucture: **PropertyFrom** and **AllPropertiesWithCascadeDeleteFrom**.
  Existing **AllPropertiesFrom** modified to not include cloning CascadeDelete concepts.

Internal improvements:

* Modifies DSL parser to allow disambiguation of similar concepts with the same name (AllProperties, e.g.) depending on the context (Logging, Persisted, History, e.g.).

0.9.1 (2013-06-05)
------------------

Breaking changes:

* Removed obsolete SOAP command *VerifyAuthorizationCommand*.

New features:

* New concept: **ModificationTimeOf**. It automatically updates the modification time of the given property.
* New concept: **CreationTime**. It automatically sets the record's creation time.
* New concept: **SqlObject**, for clean creation and deletion of any type for SQL object (such as an SQL Server job) through a DSL script.
  Use SqlDependsOn to set dependencies to other entities or properties.
  Use SqlDependsOnSqlObject to set dependencies of other DSL objects to an SqlObject.
* New concept: **ComputeForNewBaseItemsWithFilterInfo**, similar to KeepSynchronizedWithFilteredSaveInfo.

Internal improvements:

* Better performance of permission checking.
* Bugfix: Installation package did not contain Global.asax.
* Bugfix: KeepSynchronizedInfo sometimes caused redundant updates for new items.
