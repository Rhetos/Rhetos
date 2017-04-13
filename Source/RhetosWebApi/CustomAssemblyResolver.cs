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
            ICollection<Assembly> baseAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return baseAssemblies;
        }
    }
}