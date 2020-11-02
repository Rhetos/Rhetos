This project (CommonConcepts.Test) cannot be built unless the Rhetos nuget packages are built that are located in the Install folder.

To run this project's tests do the following:
1. Run the Build.bat script
2. Make sure CommonConcepts\CommonConcepts.Test contains the file rhetos-app.local.settings.json:
    {
        "ConnectionStrings": {
            "ServerConnectionString": {
                "ConnectionString": "Data Source=Server;Initial Catalog=Database;Integrated Security=SSPI;Network Library=dbmslpcn;"
            }
        }
    }
3. Build the CommonConcepts.Test project and run its tests.
