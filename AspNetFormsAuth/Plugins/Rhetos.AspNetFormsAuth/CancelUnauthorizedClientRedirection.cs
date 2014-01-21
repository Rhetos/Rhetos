/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rhetos.AspNetFormsAuth
{
    /// <summary>
    /// ASP.NET forms authentication will automatically return 302 (redirection to login.aspx) instead of 401 (unauthorized). It will at the end result with 404 on Login.aspx.
    /// This module enforces 401 return code.
    /// </summary>
    public class CancelUnauthorizedClientRedirection : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.EndRequest += new EventHandler(this.OnEndRequest);
        }

        public void Dispose()
        {
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;

            string defaultRedirectLocation = app.Request.ApplicationPath + "/login.aspx"; // Unless explicitly specified in web.config.

            if (app.Response.StatusCode == 302 // Redirect to Login web page
                && app.Response.IsRequestBeingRedirected
                && app.Response.RedirectLocation.StartsWith(defaultRedirectLocation))
            {
                if (app.Request.AppRelativeCurrentExecutionFilePath == "~/" || app.Request.AppRelativeCurrentExecutionFilePath == "~")
                {
                    // Accessing home page, redirect to login page.
                    app.Response.RedirectLocation = app.Request.ApplicationPath + @"/Resources/AspNetFormsAuth/Login.html" + app.Response.RedirectLocation.Substring(defaultRedirectLocation.Length);
                }
                else
                {
                    // Return the unauthorized HTTP status code.
                    app.Response.ClearHeaders();
                    app.Response.ClearContent();
                    app.Response.StatusCode = 401;
                }
            }
        }
    }
}