This project (CommonConcepts.Test) cannot be built unless Rhetos server is configured and CommonConceptsTest package deployed.

To initially run this project's tests do the following:
1. Build the Rhetos.sln solution.
2. Set the connection string for the test database in Rhetos\bin\ConnectionStrings.config.
3. Make sure Rhetos\RhetosPackages.config contains the following lines:
	<package id="Rhetos.CommonConcepts" source="..\..\CommonConcepts" />
    <package id="Rhetos.CommonConceptsTest" source="..\..\CommonConcepts\CommonConceptsTest" />
4. Run DeployPackages.exe.
5. Build the CommonConcepts.Test project and run its tests.

After that, to run this project's tests after some changes in Rhetos framework or CommonConcepts package:
4. Run DeployPackages.exe.
2. Build the CommonConcepts.Test project and run its tests.
