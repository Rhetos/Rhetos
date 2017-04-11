using System;
using System.Collections.Generic;
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
            //var controllersAssembly = Assembly.LoadFrom(@"F:\Project\ExternalLibrary\ExternalLibrary\bin\Debug\ExternalLibrary.dll");
            //baseAssemblies.Add(controllersAssembly);
            return baseAssemblies;
        }
    }
}