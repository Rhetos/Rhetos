using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Rhetos.Host.AspNet.Dashboard
{
    public interface IDashboardSnippet
    {
        string DisplayName { get; }
        int Order { get; }
        string RenderHtml();
    }
}
