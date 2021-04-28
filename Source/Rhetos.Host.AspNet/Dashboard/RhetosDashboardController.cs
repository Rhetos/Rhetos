using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Net.Http.Headers;

namespace Rhetos.Host.AspNet.Dashboard
{
    public class RhetosDashboardController : Controller
    {
        private readonly IEnumerable<IDashboardSnippet> _snippets;

        public RhetosDashboardController(IEnumerable<IDashboardSnippet> snippets)
        {
            _snippets = snippets;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var stringBuilder = new StringBuilder();
            foreach (var snippet in _snippets.OrderBy(a => a.Order))
            {
                stringBuilder.Append($"<h2>{snippet.DisplayName}</h2>");
                stringBuilder.Append(snippet.RenderHtml());
            }
            
            var rendered = string.Format(_html, stringBuilder);
            return Content(rendered, "text/html");
        }

        private static readonly string _html =
@"
<!DOCTYPE html>

<html>
<head>
    <meta name=""viewport"" content=""width=device-width"" />
        <title>Index</title>
        </head>
        <body>
        <h1>Rhetos Dashboard</h1>
    
        {0}
    
        </body>
    </html>

";
    }
}
