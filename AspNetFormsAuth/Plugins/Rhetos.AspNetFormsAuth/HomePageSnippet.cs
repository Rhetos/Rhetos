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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.AspNetFormsAuth
{
    [Export(typeof(IHomePageSnippet))]
    public class HomePageSnippet : IHomePageSnippet
    {
        public string Html
        {
            get
            {
                return
@"        <h2>AspNetFormsAuth</h2>
        <p>
            User authentication:
            <a href=""Resources/AspNetFormsAuth/Login.html"">Log in</a>
            <a href=""Resources/AspNetFormsAuth/Logout.html"">Log out</a>
            <a href=""Resources/AspNetFormsAuth/ChangeMyPassword.html"">Change my password</a>
        <br/>
            Administration:
            <a href=""Resources/AspNetFormsAuth/SetPassword.html"">Set password</a>
            <a href=""Resources/AspNetFormsAuth/UnlockUser.html"">Unlock user</a>
        </p>
";
            }
        }
    }
}
