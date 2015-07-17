CommonConcepts
==============

CommonConcepts is a DSL package (a plugin module) for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of **domain-specific programming language (DSL) for business applications**.  

The package contains definition of DSL concepts typically used in business applications (such as *Module*, *Entity*, *Logging*, *Computed*, *Hierarchy*, etc.)
and implementation of the concepts that generates server application.
The package contains generators for business layer object model (C# dll), database (SQL Server, Oracle), web API (IIS SOAP), ORM (Entity Framework) and other parts.

The DSL defined by CommonConcepts is extended by other Rhetos DSL packages.
The other packages may extend the language with new business concepts or new technology implementations (REST web API, OData, ASP.NET MVC model, etc.).
