# Rhetos.MsSql.Shared

This project contains the common classes and resources for **Microsoft SQL Server provider** for Rhetos and CommonConcepts.

Projects Rhetos.MsSql and Rhetos.MsSqlEf6 share the source from this projects, instead of referencing this project,
because `Rhetos.MsSql` uses the SQL classes from `Microsoft.Data.SqlClient` library (for compatibility with **EF Core**),
while `Rhetos.MsSqlEf6` uses the same SQL classes from the older `System.Data.SqlClient` library (for compatibility with **EF6**).

See the GlobalUsings.cs files in Rhetos.MsSql and Rhetos.MsSqlEf6; they provide different namespaces for SqlConnection, SqlException and other classes
that are used in this project.
