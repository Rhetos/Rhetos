using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http.Dispatcher;

namespace RhetosWebApi
{
    public class CustomAssemblyResolver : IAssembliesResolver
    {
        public ICollection<Assembly> GetAssemblies()
        {
            List<Assembly> baseAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            //string generatedPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin", "Generated");
            //foreach (string dll in Directory.GetFiles(generatedPath, "*.dll", SearchOption.AllDirectories))
            //{
            //    var loadedAssembly = Assembly.LoadFile(dll);
            //    baseAssemblies.Add(loadedAssembly);
            //}
            var controllersAssembly = Assembly.LoadFrom(@"D:\Project\RhetosWebApiMigration\Source\RhetosWebApi\bin\Generated\ApiService.dll");
            baseAssemblies.Add(controllersAssembly);
            return baseAssemblies;
        }
    }
}