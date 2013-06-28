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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Dsl;
using Rhetos.Compiler;
using System.IO;
using NHibernate.Cfg;
using System.Reflection;
using System.Diagnostics.Contracts;
using Rhetos.Dom;
using Rhetos.Extensibility;
using System.Text.RegularExpressions;
using Rhetos.Logging;

namespace Rhetos.Persistence.NHibernate
{
    public class NHibernateMappingGenerator : INHibernateMapping
    {
        public const string AssemblyTag = "<!--assemblyName-->";

        private readonly ICodeGenerator _codeGenerator;
        private readonly IPluginRepository<IConceptMappingCodeGenerator> _plugins;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly ILogger _performanceLogger;

        public NHibernateMappingGenerator(
            ICodeGenerator codeGenerator,
            IPluginRepository<IConceptMappingCodeGenerator> plugins,
            IDomainObjectModel domainObjectModel,
            ILogProvider logProvider)
        {
            _plugins = plugins;
            _codeGenerator = codeGenerator;
            _domainObjectModel = domainObjectModel;
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        private static string _mapping;
        private static readonly object _mappingLock = new object();

        public string GetMapping()
        {
            lock (_mappingLock)
            {
                if (_mapping == null)
                    _mapping = GenerateMapping();
            }
            return _mapping;
        }

        private string GenerateMapping()
        {
            var sw = Stopwatch.StartNew();

            string innerXml = _codeGenerator.ExecutePlugins(_plugins, "<!--", "-->", null).GeneratedCode;
            innerXml = innerXml.Replace(AssemblyTag, _domainObjectModel.ObjectModel.FullName);
            innerXml = Regex.Replace(innerXml, "<!--(.*)-->\\r?\\n?", string.Empty);

            string xml =
@"<?xml version=""1.0"" encoding=""utf-16"" ?>
<hibernate-mapping xmlns=""urn:nhibernate-mapping-2.2"" auto-import=""false"">
" + innerXml + @"
</hibernate-mapping>";

            _performanceLogger.Write(sw, "NHibernateMappingGenerator.GenerateMapping");
            return xml;
        }
    }
}
