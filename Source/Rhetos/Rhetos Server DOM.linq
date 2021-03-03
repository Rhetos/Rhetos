<Query Kind="Program">
  <Reference Relative="bin\Autofac.dll">bin\Autofac.dll</Reference>
  <Reference Relative="bin\EntityFramework.dll">bin\EntityFramework.dll</Reference>
  <Reference Relative="bin\EntityFramework.SqlServer.dll">bin\EntityFramework.SqlServer.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Model.dll">bin\Generated\ServerDom.Model.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Orm.dll">bin\Generated\ServerDom.Orm.dll</Reference>
  <Reference Relative="bin\Generated\ServerDom.Repositories.dll">bin\Generated\ServerDom.Repositories.dll</Reference>
  <Reference Relative="bin\NLog.dll">bin\NLog.dll</Reference>
  <Reference Relative="bin\Oracle.ManagedDataAccess.dll">bin\Oracle.ManagedDataAccess.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.AspNetFormsAuth.dll">bin\Plugins\Rhetos.AspNetFormsAuth.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.dll">bin\Plugins\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll">bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dsl.DefaultConcepts.dll">bin\Plugins\Rhetos.Dsl.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll">bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Configuration.Autofac.dll">bin\Rhetos.Configuration.Autofac.dll</Reference>
  <Reference Relative="bin\Rhetos.Dom.Interfaces.dll">bin\Rhetos.Dom.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Dsl.Interfaces.dll">bin\Rhetos.Dsl.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Interfaces.dll">bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Logging.Interfaces.dll">bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.Interfaces.dll">bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Processing.Interfaces.dll">bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Security.Interfaces.dll">bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.TestCommon.dll">bin\Rhetos.TestCommon.dll</Reference>
  <Reference Relative="bin\Rhetos.Utilities.dll">bin\Rhetos.Utilities.dll</Reference>
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
  <Namespace>Rhetos</Namespace>
</Query>

void Main()
{
	string applicationFolder = Path.GetDirectoryName(Util.CurrentQueryPath);
	ConsoleLogger.MinLevel = EventType.Info; // Use EventType.Trace for more detailed log.
	
	using (var scope = ProcessContainer.CreateScope(applicationFolder))
    {
        var context = scope.Resolve<Common.ExecutionContext>();
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
		
		//scope.CommitAndClose(); // Database transaction is rolled back by default.
    }
}