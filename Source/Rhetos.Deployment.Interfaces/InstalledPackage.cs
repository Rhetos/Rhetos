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
using System.Text;

namespace Rhetos.Deployment
{
    public class InstalledPackage
    {
        public InstalledPackage(
            string id,
            string version,
            IEnumerable<PackageRequest> dependencies,
            string folder,
            PackageRequest request,
            string source,
            string requiredRhetosVersion)
        {
            Id = id;
            Version = version;
            Dependencies = dependencies;
            Folder = folder;
            Request = request;
            Source = source;
            RequiredRhetosVersion = requiredRhetosVersion;
        }

        public string Id { get; private set; }

        public string Version { get; private set; }

        public IEnumerable<PackageRequest> Dependencies { get; private set; }

        /// <summary>The local folder where the package files are extracted and used by Rhetos.</summary>
        public string Folder { get; private set; }

        public PackageRequest Request { get; private set; }

        /// <summary>URI or a folder where the package was downloaded from.</summary>
        public string Source { get; set; }

        /// <summary>A nuget-compatible version specification, or null.</summary>
        public string RequiredRhetosVersion { get; private set; }

        public string Report()
        {
            return Id + " " + Version + " (requested from " + Request.RequestedBy + ") in " + Folder + ".";
        }
    }
}
