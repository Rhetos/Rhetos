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

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace Rhetos
{
    /// <summary>
    /// Initialize plugin web service. Called at run-time.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Called only once when initializing the server process (see Global.asax: Application_Start).
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called once for each System.Web.HttpApplication instance in the server process (see Global.asax: HttpApplication.Init).
        /// </summary>
        void InitializeApplicationInstance(HttpApplication context);
    }
}
