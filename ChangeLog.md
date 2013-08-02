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
