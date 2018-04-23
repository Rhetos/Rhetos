# CommonConcepts

CommonConcepts is a DSL plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It provides an implementation of **domain-specific programming language (DSL) for business applications**.

The package contains definition of DSL concepts typically used in business applications (such as *Module*, *Entity*, *Logging*, *Computed*, *Hierarchy*, etc.)
and implementation of the concepts that generates server application.
The package contains generators for business layer object model (C# dll), database (SQL Server, Oracle), web API (IIS SOAP), ORM (Entity Framework) and other parts.

The DSL defined by CommonConcepts is extended by other Rhetos DSL packages.
The other packages may extend the language with new business concepts or new technology implementations (REST web API, OData, ASP.NET MVC model, etc.).

## Build

**Note:** This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
You don't need to build it from source in order to use it in your application.

To build the package from source, run `Build.bat`.
The script will pause in case of an error.
The build output is a NuGet package in the "Install" subfolder.

## Installation

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.CommonConcepts**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  It can be downloaded or installed directly from there.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).
