<Query Kind="Program">
  <Reference Relative="bin\Autofac.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Autofac.dll</Reference>
  <Reference Relative="bin\EntityFramework.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\EntityFramework.dll</Reference>
  <Reference Relative="bin\EntityFramework.SqlServer.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\EntityFramework.SqlServer.dll</Reference>
  <Reference Relative="bin\NLog.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\NLog.dll</Reference>
  <Reference Relative="bin\Oracle.ManagedDataAccess.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Oracle.ManagedDataAccess.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.AspNetFormsAuth.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.AspNetFormsAuth.dll</Reference>
  <Reference Relative="bin\Rhetos.Configuration.Autofac.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Configuration.Autofac.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Dom.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Dom.Interfaces.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dsl.DefaultConcepts.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dsl.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Rhetos.Dsl.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Dsl.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Logging.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Processing.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Security.Interfaces.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.TestCommon.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.TestCommon.dll</Reference>
  <Reference Relative="bin\Rhetos.Utilities.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Rhetos.Utilities.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Model.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Generated\ServerDom.Model.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Orm.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Generated\ServerDom.Orm.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Repositories.dll">C:\My Projects\Rhetos\Source\Rhetos\bin\Generated\ServerDom.Repositories.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Namespace>Oracle.ManagedDataAccess.Client</Namespace>
  <Namespace>Rhetos.Configuration.Autofac</Namespace>
  <Namespace>Rhetos.Dom</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Dsl</Namespace>
  <Namespace>Rhetos.Dsl.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Logging</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Data.Entity</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
  <Namespace>System.IO</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Reflection</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>System.Text</Namespace>
  <Namespace>System.Xml</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
  <Namespace>Autofac</Namespace>
  <Namespace>Rhetos.TestCommon</Namespace>
</Query>

void Main()
{
    ConsoleLogger.MinLevel = EventType.Info; // Use "Trace" for more detailed log.
    var rhetosServerPath = Path.GetDirectoryName(Util.CurrentQueryPath);
    Directory.SetCurrentDirectory(rhetosServerPath);
    // Set commitChanges parameter to COMMIT or ROLLBACK the data changes.
    using (var container = new RhetosTestContainer(commitChanges: false))
    {
        var context = container.Resolve<Common.ExecutionContext>();
        var repository = context.Repository;

        // Query data from the `Common.Claim` table:
        
        var claims = repository.Common.Claim.Query()
            .Where(c => c.ClaimResource.StartsWith("Common.") && c.ClaimRight == "New")
            .ToSimple(); // Removes ORM navigation properties from the loaded objects.
            
        claims.ToString().Dump("Common.Claims SQL query");
        claims.Dump("Common.Claims items");
        
        // Add and remove a `Common.Principal`:
        
        var testUser = new Common.Principal { Name = "Test123", ID = Guid.NewGuid() };
        repository.Common.Principal.Insert(new[] { testUser });
        repository.Common.Principal.Delete(new[] { testUser });
        
        // Print logged events for the `Common.Principal`:
        
        repository.Common.LogReader.Query()
            .Where(log => log.TableName == "Common.Principal" && log.ItemId == testUser.ID)
            .ToList()
            .Dump("Common.Principal log");
            
        Console.WriteLine("Done.");
    }
}