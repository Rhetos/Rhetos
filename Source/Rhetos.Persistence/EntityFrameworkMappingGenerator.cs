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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Persistence
{
    public class EntityFrameworkMappingGenerator : IGenerator
    {
        private const string _segmentSplitter = "<!--SegmentSplitter-->";
        private readonly ICodeGenerator _codeGenerator;
        private readonly IPluginsContainer<IConceptMapping> _plugins;
        private readonly ILogger _performanceLogger;

        public EntityFrameworkMappingGenerator(
            ICodeGenerator codeGenerator,
            IPluginsContainer<IConceptMapping> plugins,
            ILogProvider logProvider)
        {
            _plugins = plugins;
            _codeGenerator = codeGenerator;
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        private Lazy<string> GetProviderManifestToken = new Lazy<string>(() => MsSqlUtility.GetProviderManifestToken());
        
        public void Generate()
        {
            var sw = Stopwatch.StartNew();

            string xml = _codeGenerator.ExecutePlugins(_plugins, "<!--", "-->", new InitialSnippet()).GeneratedCode;
            string[] segments = xml.Split(new[] { _segmentSplitter }, StringSplitOptions.None);

            if (segments.Count() != EntityFrameworkMapping.ModelFiles.Count())
                throw new FrameworkException("Unexpected number of metadata segments: " + segments.Count() + ", expected " + EntityFrameworkMapping.ModelFiles.Count() + ".");
            
            for (int s = 0; s < segments.Count(); s++)
            {
                string clearedXml = XmlUtility.RemoveComments(segments[s]);
                if (!string.IsNullOrWhiteSpace(clearedXml))
                {
                    clearedXml = string.Format(GetXmlRootElements()[s], clearedXml);
                    string filePath = Path.Combine(Paths.GeneratedFolder, EntityFrameworkMapping.ModelFiles[s]);
                    File.WriteAllText(filePath, clearedXml, Encoding.UTF8);
                }
            }

            _performanceLogger.Write(sw, "EntityFrameworkMappingGenerator.GenerateMapping");
        }

        private string[] GetXmlRootElements()
        {
                return new[]
                {
@"<Schema Namespace=""Rhetos"" Alias=""Self"" annotation:UseStrongSpatialTypes=""false"" xmlns:annotation=""http://schemas.microsoft.com/ado/2009/02/edm/annotation"" xmlns:customannotation=""http://schemas.microsoft.com/ado/2013/11/edm/customannotation"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm"">
{0}
</Schema>",
@"<Mapping Space=""C-S"" xmlns=""http://schemas.microsoft.com/ado/2009/11/mapping/cs"">
{0}
</Mapping>",
@"<Schema Namespace=""Rhetos"" Provider=""System.Data.SqlClient"" ProviderManifestToken=""" + GetProviderManifestToken.Value + "\"" + @" Alias=""Self"" xmlns:customannotation=""http://schemas.microsoft.com/ado/2013/11/edm/customannotation"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm/ssdl"">
{0}
</Schema>"
            };
        }

        private class InitialSnippet : IConceptCodeGenerator
        {
            public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
            {
                codeBuilder.InsertCode(string.Join("\r\n", new[]
                {
                    EntityFrameworkMapping.ConceptualModelTag,
                    _segmentSplitter,
                    EntityFrameworkMapping.MappingTag,
                    _segmentSplitter,
                    EntityFrameworkMapping.StorageModelTag
                }));
            }
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }
    }
}
