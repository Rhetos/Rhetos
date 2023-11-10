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

using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace Rhetos.DatabaseGenerator
{
    public class CoreSqlResourcesPlugin : ISqlResourcesPlugin
    {
        private readonly string _databaseLanguage;

        public CoreSqlResourcesPlugin(DatabaseSettings databaseSettings)
        {
            _databaseLanguage = databaseSettings.DatabaseLanguage;
        }

        public IDictionary<string, string> GetResources()
        {
            Type sampleType = GetType();
            string resourceName = sampleType.Namespace + ".Sql." + _databaseLanguage;
            var resourceAssembly = sampleType.Assembly;
            var resourceManager = new ResourceManager(resourceName, resourceAssembly);
            ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (resourceSet == null)
                return null;

            var result = new Dictionary<string, string>();
            foreach (var entry in resourceSet.Cast<DictionaryEntry>())
                result.Add((string)entry.Key, (string)entry.Value);
            return result;
        }
    }
}
