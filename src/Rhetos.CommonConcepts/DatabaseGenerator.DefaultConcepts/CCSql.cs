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
using System.Globalization;
using System.Resources;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    public static class Sql
    {
        private static Lazy<ResourceManager> _resourceManager = new(() => Rhetos.DatabaseGenerator.Sql.CreateResourceManager(typeof(Sql)));

        public static string TryGet(string resourceName) => _resourceManager.Value.GetString(resourceName);

        public static string Get(string resourceName) => TryGet(resourceName) ?? throw new FrameworkException($"Missing SQL resource '{resourceName}' for the database language of resource '{_resourceManager.Value.BaseName}'.");

        public static string Format(string resourceName, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(resourceName), args);
    }
}
