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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    /// <summary>
    /// The generated EntityFrameworkContext will work with or without these metadata files,
    /// but context initialization is faster when loading metadata from the pregenerated files.
    /// </summary>
    [Export(typeof(IGenerator))]
    public class EntityFrameworkGenerateMetadataFiles : IGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly IDomainObjectModel _dom;
        private readonly ConnectionString _connectionString;
        private readonly GeneratedFilesCache _cache;
        private readonly IConfiguration _configuration;

        public EntityFrameworkGenerateMetadataFiles(
            ILogProvider logProvider, 
            IDomainObjectModel dom, 
            ConnectionString connectionString, 
            GeneratedFilesCache cache,
            IConfiguration configuration)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _dom = dom;
            _connectionString = connectionString;
            _cache = cache;
            _configuration = configuration;
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        public void Generate()
        {
            var sw = Stopwatch.StartNew();

            byte[] sourceHash = GetOrmHash();
            string sampleEdmFile = Path.Combine(Paths.GeneratedFolder, EntityFrameworkMetadata.SegmentsFromCode.First().FileName);
            var edmExtensions = EntityFrameworkMetadata.SegmentsFromCode.Select(s => Path.GetExtension(s.FileName));

            if (_cache.RestoreCachedFiles(sampleEdmFile, sourceHash, Paths.GeneratedFolder, edmExtensions) != null)
            {
                _performanceLogger.Write(sw, "EntityFrameworkMetadata: Restore cached EDM files.");
            }
            else
            {
                var connection = new SqlConnection(_connectionString);

                var dbConfiguration = (DbConfiguration)_dom.GetType("Common.EntityFrameworkConfiguration")
                    .GetConstructor(new Type[] { })
                    .Invoke(new object[] { });

                var dbContext = (DbContext)_dom.GetType("Common.EntityFrameworkContext")
                    .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(DbConnection), dbConfiguration.GetType(), typeof(IConfiguration) }, null)
                    .Invoke(new object[] { connection, dbConfiguration, _configuration });

                string edmx;
                using (var stringWriter = new StringWriter())
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    xmlWriter.Formatting = System.Xml.Formatting.Indented;
                    EdmxWriter.WriteEdmx(dbContext, xmlWriter);
                    edmx = stringWriter.ToString();
                }

                _performanceLogger.Write(sw, "EntityFrameworkMetadata: Extract EDMX.");

                foreach (var segment in EntityFrameworkMetadata.SegmentsFromCode)
                {
                    string startTag = "\r\n    <" + segment.TagName + ">\r\n";
                    string endTag = "\r\n    </" + segment.TagName + ">\r\n";

                    int start = edmx.IndexOf(startTag, StringComparison.Ordinal);
                    int end = edmx.IndexOf(endTag, StringComparison.Ordinal);
                    int alternativeStart = edmx.IndexOf(startTag, start + 1, StringComparison.Ordinal);
                    int alternativeEnd = edmx.IndexOf(endTag, end + 1, StringComparison.Ordinal);
                    if (start == -1 || alternativeStart != -1 || end == -1 || alternativeEnd != -1)
                        throw new Exception("Unexpected EDMX format. " + segment.TagName + " tag locations: start=" + start + " alternativeStart=" + alternativeStart + " end=" + end + " alternativeEnd=" + alternativeEnd + ".");

                    string segmentXml = edmx.Substring(start + startTag.Length, end - start - startTag.Length);
                    File.WriteAllText(Path.Combine(Paths.GeneratedFolder, segment.FileName), segmentXml, Encoding.UTF8);
                }

                _performanceLogger.Write(sw, "EntityFrameworkMetadata: Save EDM files.");
            }

            _cache.SaveHash(sampleEdmFile, sourceHash);
        }

        private byte[] GetOrmHash()
        {
            var hashes = new[]
            {
                _cache.LoadHash(Paths.GetDomAssemblyFile(DomAssemblies.Model)),
                _cache.LoadHash(Paths.GetDomAssemblyFile(DomAssemblies.Orm)),
                // TODO: Add DatabaseGenerator hash for new created ConceptApplications.
                _cache.GetHash(MsSqlUtility.GetProviderManifestToken())
            };
            return _cache.JoinHashes(hashes);
        }
    }
}
