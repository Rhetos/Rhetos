# KeepSynchronized deployment regression test

`KeepSynchronizedRecomputeOnDeployDeploymentTest` runs in the SQL Server/EF Core
test suite (`CommonConcepts.Test.MsSql`) and has the `Deployment` test category.

## Prerequisites

- Build the local Rhetos NuGet packages and the test app with the same version,
  following the repository's normal build instructions. The test pins every
  Rhetos package to that version and verifies the CommonConcepts binary hashes.
- Install the .NET SDK and make `dotnet` available on `PATH`.
- Configure the test app's SQL Server connection as usual. The account must also
  have permission to create and drop databases on that server.
- NuGet dependencies must be available from the cache or NuGet.org.

## Running only this test

Build the test assembly without deploying its referenced test app:

```powershell
dotnet build test/CommonConcepts.Test.MsSql/CommonConcepts.Test.MsSql.csproj --no-restore /p:BuildProjectReferences=false /p:RhetosDeploy=false
dotnet test test/CommonConcepts.Test.MsSql/CommonConcepts.Test.MsSql.csproj --no-build --no-restore --filter "FullyQualifiedName~KeepSynchronizedRecomputeOnDeployDeploymentTest"
```

## Isolation and assertions

The test creates a minimal temporary application and a GUID-named database
(`RhetosKeepSyncDeployment_*`) on the configured SQL Server. It does not deploy
to or change the shared test database. Fixture builds explicitly disable
automatic deployment; database updates run through the real Rhetos CLI.

The first deployment has a source with both values and a target mapping only
`Name`. The second build adds `Added` only on the target, without restoring or
changing the Rhetos packages. Its deployment must populate the new property on
the existing target record and update the synchronization metadata. The source
view definition and the source part of the context must remain unchanged.

A third deployment with no changes must not recompute the target. Each child
process has a five-minute timeout and captures its output for diagnostics.
The test drops only its own newly created database and deletes its temporary
application in `finally`, including when an assertion or deployment fails.
