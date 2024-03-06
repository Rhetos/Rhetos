# Rhetos.CommonConcepts.Test

Rhetos.CommonConcepts.Test contains unit tests for classes in Rhetos.CommonConcepts.

Some tests depend on ORM and database provider. They should use both EF6 and EF Core implementations
(Rhetos.MsSqlEf6 and Rhetos.MsSql) for each test case. The tests assumes **Microsoft SQL Server** client libraries.

A detailed integration testing for both EF 6 and EF Core is implemented in solution **CommonConceptsTest.sln**.
