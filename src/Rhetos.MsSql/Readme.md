# Rhetos.MsSql

Rhetos.MsSql is the Microsoft SQL Server provider for Rhetos and CommonConcepts, with EF Core support.

Remarks:

1. This project's NuGet package provides both **ORM provider** (Rhetos.EfCore) and **database provider** (from this project) for Rhetos.
   The two providers have independent implementations, but are combined in a single project to reduce clutter.

2. The SQL Server database provider shares the source with **Rhetos.MsSqlEf6** package (the source is in Rhetos.MsSql.Shared),
   since the implementation is the same, but `Rhetos.MsSql` uses the SQL classes from the newer `Microsoft.Data.SqlClient` library,
   while `Rhetos.MsSqlEf6` uses the same SQL classes from the older `System.Data.SqlClient` library.
   The System.Data.SqlClient is used for compatibility with EF6.
