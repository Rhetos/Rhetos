# CommonConcepts

CommonConcepts is a DSL plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of **domain-specific programming language (DSL) for business applications**.

The package contains definition of DSL concepts typically used in business applications (such as *Module*, *Entity*, *Logging*, *Computed*, *Hierarchy*, etc.)
and implementation of the concepts that generates server application.
The package contains generators for business layer object model (C# dll), database, web API, ORM and other parts.
There are Rhetos plugins available for different database providers (MS SQL Server, PostgreSql, ...) and web API technologies (JSON REST, SOAP XML, OData, ...).

The DSL defined by CommonConcepts is extended by other Rhetos DSL packages.
The other packages may extend the language with new business concepts or new technology implementations.

## Installation

Installing this package to a Rhetos application:

1. Add "Rhetos.CommonConcepts" NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.

## How to contribute

See Rhetos framework's [Readme.md](https://github.com/Rhetos/Rhetos/blob/master/Readme.md)
for contribution and build instructions.
