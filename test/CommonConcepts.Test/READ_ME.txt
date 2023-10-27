This project (CommonConcepts.Test) cannot be built unless the Rhetos NuGet packages are built that are located in the Install folder (result of Build.bat)
and database connection string configured. See instructions below.

Initial setup:

1. Create an empty database (for example, "Rhetos" database on "localhost" SQL Server instance).
2. Make sure test\CommonConcepts.TestApp contains the file "rhetos-app.local.settings.json" with connection string:
   Copy the file from tools\Configuration\Template.rhetos-app.local.settings.json,
   and edit SQL Server instance name (localhost, e.g.) and database name (Rhetos, e.g.).

To build and run this project's tests:

1. Run the Build.bat script.
2. Run the Test.bat script, or run tests in Visual Studio.
