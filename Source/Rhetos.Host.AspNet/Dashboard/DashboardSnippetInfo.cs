using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rhetos.Host.AspNet.Dashboard
{
    public class DashboardSnippetInfo
    {
        public string DisplayName { get;set; }
        public Type ViewComponentType { get;set; }
        public int Order { get;set; }
    }
}
