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

using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class EntityFrameworkMetadata
    {
        private readonly ILogger _performanceLogger;
        private MetadataWorkspace _metadataWorkspace;
        private bool _initialized;
        private object _initializationLock = new object();

        public EntityFrameworkMetadata(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        /// <summary>
        /// Returns null if the pre-generated metadata is not available.
        /// </summary>
        public MetadataWorkspace MetadataWorkspace
        {
            get
            {
                if (!_initialized)
                    lock (_initializationLock)
                        if (!_initialized)
                        {
                            var sw = Stopwatch.StartNew();

                            var filesFromCode = _segmentsFromCode.Select(segment => segment.FileName)
                                .Select(fileName => Path.Combine(Paths.GeneratedFolder, fileName))
                                .ToList();

                            if (File.Exists(filesFromCode.First()))
                            {
                                var filesFromGenerator = EntityFrameworkMappingGenerator.ModelFiles
                                    .Select(fileName => Path.Combine(Paths.GeneratedFolder, fileName));
                                var loadFiles = filesFromGenerator.Concat(filesFromCode)
                                    .Where(file => File.Exists(file))
                                    .ToList();
                                _metadataWorkspace = new MetadataWorkspace(loadFiles, new Assembly[] { });
                                _performanceLogger.Write(sw, "EntityFrameworkMetadata: Load EDM files.");
                            }
                            else
                                _metadataWorkspace = null;

                            _initialized = true;
                        }

                return _metadataWorkspace;
            }
        }

        private class Segment
        {
            public string TagName;
            public string FileName;
        }

        private static readonly Segment[] _segmentsFromCode = new Segment[]
        {
            new Segment { FileName = "ServerDomEdmFromCode.csdl", TagName = "ConceptualModels" },
            new Segment { FileName = "ServerDomEdmFromCode.msl", TagName = "Mappings" },
            new Segment { FileName = "ServerDomEdmFromCode.ssdl", TagName = "StorageModels" },
        };

        public void SaveMetadata(DbContext dbContext)
        {
            var sw = Stopwatch.StartNew();

            string edmx;
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.Formatting = System.Xml.Formatting.Indented;
                EdmxWriter.WriteEdmx(dbContext, xmlWriter);
                edmx = stringWriter.ToString();
            }

            _performanceLogger.Write(sw, "EntityFrameworkMetadata: Extract EDMX.");

            foreach (var segment in _segmentsFromCode)
            {
                string startTag = "\r\n    <" + segment.TagName + ">\r\n";
                string endTag = "\r\n    </" + segment.TagName + ">\r\n";

                int start = edmx.IndexOf(startTag, StringComparison.Ordinal);
                int end = edmx.IndexOf(endTag, StringComparison.Ordinal);
                int alternativeStart = edmx.IndexOf(startTag, start + 1, StringComparison.Ordinal);
                int alternativeEnd = edmx.IndexOf(endTag, end + 1, StringComparison.Ordinal);
                if (start == -1 || alternativeStart != -1 || end == -1 || alternativeEnd != -1)
                    throw new Exception("Unexcepted EDMX format. " + segment.TagName + " tag locations: start=" + start + " alternativeStart=" + alternativeStart + " end=" + end + " alternativeEnd=" + alternativeEnd + ".");

                string segmentXml = edmx.Substring(start + startTag.Length, end - start - startTag.Length);
                File.WriteAllText(Path.Combine(Paths.GeneratedFolder, segment.FileName), segmentXml, Encoding.UTF8);
            }

            _performanceLogger.Write(sw, "EntityFrameworkMetadata: Save EDM files.");
        }
    }
}
