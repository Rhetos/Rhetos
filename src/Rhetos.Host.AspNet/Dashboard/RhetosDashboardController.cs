/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            foreach (var snippet in _snippets.OrderBy(a => a.Order).ThenBy(a => a.DisplayName).ThenBy(a => a.GetType().FullName))
            {
                stringBuilder.AppendLine($"    <h2>{snippet.DisplayName}</h2>");
                stringBuilder.AppendLine(snippet.RenderHtml().TrimEnd());
                stringBuilder.AppendLine();
            }

            string rendered = string.Format(null, _html, stringBuilder);
            return Content(rendered, "text/html; charset=utf-8");
        }

        private static readonly CompositeFormat _html = CompositeFormat.Parse(
@"<!DOCTYPE html>
<html>
  <head>
    <meta name=""viewport"" content=""width=device-width"" />
    <title>Index</title>
  </head>
  <body>
    <h1>Rhetos Dashboard</h1>
    
{0}  </body>
</html>
");
    }
}
