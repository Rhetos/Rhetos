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
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Web;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Autofac;
using Rhetos.Utilities;
using System.Runtime.Serialization;
using Autofac.Integration.Wcf;
using Rhetos.Logging;
using System.Diagnostics;

namespace Rhetos.Web
{
    /// <summary>
    /// Initialize plugin web service. Called at run-time.
    /// </summary>
    public class WebServices
    {
        private readonly IEnumerable<IService> _services;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public WebServices(IEnumerable<IService> services, ILogProvider logProvider)
        {
            _services = services;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        /// <summary>
        /// Call only once when initializing the server process (see Global.asax: Application_Start).
        /// </summary>
        public void Initialize()
        {
            foreach (var service in _services)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    service.Initialize();
                    _performanceLogger.Write(stopwatch, $"Service {service.GetType().FullName}.Initialize");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                    throw;
                }
            }
        }

        /// <summary>
        /// Call once for each System.Web.HttpApplication instance in the server process (see Global.asax: HttpApplication.Init).
        /// </summary>
        public void InitializeApplicationInstance(HttpApplication context)
        {
            if (_services != null)
                foreach (var service in _services)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        service.InitializeApplicationInstance(context);
                        _performanceLogger.Write(stopwatch, $"Service {service.GetType().FullName}.InitializeApplicationInstance");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.ToString());
                        throw;
                    }
                }
        }
    }
}