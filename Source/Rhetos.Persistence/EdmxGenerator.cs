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
    public class EdmxGenerator : IGenerator
    {
        public static readonly string[] ModelFiles = new[] { "ServerDomEdmFromCode2.csdl", "ServerDomEdmFromCode2.msl", "ServerDomEdmFromCode2.ssdl" };

        private readonly ICodeGenerator _codeGenerator;
        private readonly IPluginsContainer<IEdmxCodeGenerator> _plugins;
        private readonly ILogger _performanceLogger;

        public EdmxGenerator(
            ICodeGenerator codeGenerator,
            IPluginsContainer<IEdmxCodeGenerator> plugins,
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
            var a = _plugins.GetPlugins().Count();
            string xml = _codeGenerator.ExecutePlugins(_plugins, "<!--", "-->", new EdmxInitialCodeSnippet()).GeneratedCode;
            string[] segments = xml.Split(new[] { EdmxInitialCodeSnippet.SegmentSplitter }, StringSplitOptions.None);

            if (segments.Count() != ModelFiles.Count())
                throw new FrameworkException("Unexpected number of metadata segments: " + segments.Count() + ", expected " + ModelFiles.Count() + ".");

            for (int s = 0; s < segments.Count(); s++)
            {
                string clearedXml = XmlUtility.RemoveComments(segments[s]);
                if (!string.IsNullOrWhiteSpace(clearedXml))
                {
                    clearedXml = string.Format(GetXmlRootElements()[s], clearedXml);
                    string filePath = Path.Combine(Paths.GeneratedFolder, ModelFiles[s]);
                    File.WriteAllText(filePath, clearedXml, Encoding.UTF8);
                }
            }

            _performanceLogger.Write(sw, "EntityFrameworkMappingGenerator.GenerateMapping");
        }

        private string[] GetXmlRootElements()
        {
            return new[]
            {
@"<Schema Namespace=""Common"" Alias=""Self"" annotation:UseStrongSpatialTypes=""false"" xmlns:annotation=""http://schemas.microsoft.com/ado/2009/02/edm/annotation"" xmlns:customannotation=""http://schemas.microsoft.com/ado/2013/11/edm/customannotation"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm"">{0}
</Schema>",
@"<Mapping Space=""C-S"" xmlns=""http://schemas.microsoft.com/ado/2009/11/mapping/cs"">{0}
</Mapping>",
@"<Schema Namespace=""CodeFirstDatabaseSchema"" Provider=""System.Data.SqlClient"" ProviderManifestToken=""" + GetProviderManifestToken.Value + "\"" + @" Alias=""Self"" xmlns:customannotation=""http://schemas.microsoft.com/ado/2013/11/edm/customannotation"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm/ssdl"">
{0}
</Schema>"
            };
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }
    }
}
