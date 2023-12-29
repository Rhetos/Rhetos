**Rhetos.MsSqlEf6** is the Microsoft SQL Server provider for Rhetos and CommonConcepts, with EF6 support.

For new projects use Rhetos.MsSql instead, since it uses a modern EF Core instead of EF6.

Remarks:

1. This project contains both **ORM provider** (EF6) and **database provider** (SQL Server) for Rhetos.
   The two providers have independent implementations, but are combined in a single project to reduce clutter.

2. The SQL Server database provider shares the source with **Rhetos.MsSql** package (the source is in Rhetos.MsSql.Shared),
   since the implementation is the same, but `Rhetos.MsSql` uses the SQL classes from the newer `Microsoft.Data.SqlClient` library,
   while `Rhetos.MsSqlEf6` uses the same SQL classes from the older `System.Data.SqlClient` library.
   The System.Data.SqlClient is used for compatibility with EF6.
