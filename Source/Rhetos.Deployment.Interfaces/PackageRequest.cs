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
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Rhetos.Deployment
{
    public class PackageRequest
    {
        /// <summary>Package name.</summary>
        public string Id { get; set; }

        /// <summary>(Optional) Supported versions, specified in NuGet format. If not provided, the last available version will be used.</summary>
        public string VersionsRange { get; set; }

        /// <summary>(Optional) Source where the package can be found.
        /// If provided, it will be used instead of DeploymentSources.
        /// See DeploymentSources for supported source formats.</summary>
        public string Source { get; set; }

        /// <summary>
        /// The value should not be provided in configuration file or package metadata file.
        /// It is automatically set by the system to track where the request originated.
        /// </summary>
        public string RequestedBy { get; set; }

        public string ReportIdVersionsRange()
        {
            return Id + (VersionsRange != null ? " " + VersionsRange : " (version not specified)");
        }
    }
}