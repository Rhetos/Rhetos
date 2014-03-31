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
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Configuration.Autofac
{
    public class Paths
    {
        private string _rootPath;

        public Paths(string rootPath)
        {
            _rootPath = rootPath;
        }

        public string DslScriptsFolder { get { return Path.Combine(_rootPath, "DslScripts"); } }
        public string DataMigrationScriptsFolder { get { return Path.Combine(_rootPath, "DataMigration"); } }
        public string ResourcesFolder { get { return Path.Combine(_rootPath, "Resources"); } }
        public string GeneratedFolder { get { return Path.Combine(_rootPath, "bin\\Generated"); } }

        public string RhetosServerWebConfigFile { get { return Path.Combine(_rootPath, "Web.config"); } }
        public string NHibernateMappingFile { get { return Path.Combine(_rootPath, "bin\\ServerDomNHibernateMapping.xml"); } }
        public string DomAssemblyFile { get { return Path.Combine(_rootPath, "bin", DomAssemblyName + ".dll"); } }

        public const string DomAssemblyName = "ServerDom";
    }
}
