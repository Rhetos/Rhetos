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
using System.Configuration;
using System.Globalization;
using System.Resources;
using Rhetos.Utilities;

namespace Rhetos.DatabaseGenerator
{
    public static class Sql
    {
        private static ResourceManager _resourceManager;
        private static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(_resourceManager, null))
                {
                    string resourceName = typeof(Sql).Namespace + ".Sql." + SqlUtility.DatabaseLanguage;
                    var resourceAssembly = typeof(Sql).Assembly;
                    _resourceManager = new ResourceManager(resourceName, resourceAssembly);
                }
                return _resourceManager;
            }
        }

        public static string Get(string resourceName)
        {
            var value = ResourceManager.GetString(resourceName);
            if (value == null)
                throw new FrameworkException("Missing SQL resource '" + resourceName + "' for database language '" + SqlUtility.DatabaseLanguage + "'.");
            return value;
        }

        public static string Format(string resourceName, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(resourceName), args);
        }
    }
}
