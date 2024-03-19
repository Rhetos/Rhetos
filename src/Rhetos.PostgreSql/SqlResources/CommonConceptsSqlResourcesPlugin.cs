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

using Rhetos.SqlResources;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.IO;

namespace Rhetos.PostgreSql.SqlResources
{
    public class CommonConceptsSqlResourcesPlugin : ISqlResourcesPlugin
    {

        private readonly string _databaseLanguage;

        public CommonConceptsSqlResourcesPlugin(DatabaseSettings databaseSettings)
        {
            _databaseLanguage = databaseSettings.DatabaseLanguage;
        }

        public IDictionary<string, string> GetResources()
        {
            if (!_databaseLanguage.StartsWith(PostgreSqlUtility.DatabaseLanguage))
                return null;

            var result = ResourcesUtility.ReadEmbeddedResx(Path.Combine("SqlResources", "Rhetos.CommonConcepts.Build.PostgreSql.resx"), GetType().Assembly, true);

            foreach (var keyValue in ResourcesUtility.ReadEmbeddedResx(Path.Combine("SqlResources", "Rhetos.CommonConcepts.SqlFiles.PostgreSql.resx"), GetType().Assembly, true))
                result[keyValue.Key] = keyValue.Value;

            return result;
        }
    }
}
