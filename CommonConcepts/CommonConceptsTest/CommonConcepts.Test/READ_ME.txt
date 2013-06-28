This project (CommonConcepts.Test) cannot be built unless Rhetos server is configured and CommonConceptsTest package deployed.
To initally run this project's tests do the following:
1. Build the Rhetos.sln solution.
2. Set a connection string for the test database in Rhetos\bin\ConnectionStrings.config.
3. Edit ApplyPackages.txt in Rhetos folder to include packages CommonConcepts and CommonConceptsTest (enter relative folder paths).
4. Run ApplyPackages.bat in Rhetos folder.
5. Build the CommonConcepts.Test project and run its tests.

After that, to run this project's tests atfter some changes in Rhetos framework or CommonConcepts package:
1. Run ApplyPackages.bat.
2. Build the CommonConcepts.Test project and run its tests.
