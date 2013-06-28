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
using System.IO;
using System.Xml.Serialization;

namespace Rhetos.Deployment
{
    public class Package
    {
        public string SpecificFor { get; set; }
        public string Identifier { get; set; }
        public string Version { get; set; }

        public List<Package> Dependencies { get; set; }

        [XmlArrayItem("Version")]
        public List<string> CompatibleWithPreviousVersions { get; set; }

        public static Package FromFile(string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Package));
            TextReader reader = new StreamReader(fileName);
            Package p;
            try
            {
                p = (Package) serializer.Deserialize(reader);
            }
            finally
            {
                reader.Close();
            }

            ValidatePackage(fileName, p);
            return p;
        }

        private static void ValidatePackage(string fileName, Package p)
        {
            CheckIdentifierAndVersion(fileName, p);

            if (p.Dependencies == null)
                p.Dependencies = new List<Package>();

            foreach (var dependency in p.Dependencies)
                CheckIdentifierAndVersion(fileName, dependency);
        }

        private static void CheckIdentifierAndVersion(string fileName, Package p)
        {
            if (string.IsNullOrEmpty(p.Identifier))
                throw new FrameworkException("Missing 'Identifier' element in package specification: " + fileName + ".");
            if (string.IsNullOrEmpty(p.Version))
                throw new FrameworkException("Missing 'Version' element in package specification: " + fileName + ".");
        }
    }
}