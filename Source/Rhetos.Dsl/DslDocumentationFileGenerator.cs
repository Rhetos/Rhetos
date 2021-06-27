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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Rhetos.Dsl
{
    public class DslDocumentationFileGenerator : IGenerator
    {
        private readonly DslSyntax _dslSyntax;
        private readonly DslDocumentationFile _dslDocumentationFile;
        private readonly ILogger _logger;

        public DslDocumentationFileGenerator(DslSyntax dslSyntax, ILogProvider logProvider, DslDocumentationFile dslDocumentationFile)
        {
            _dslSyntax = dslSyntax;
            _dslDocumentationFile = dslDocumentationFile;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public void Generate()
        {
            var conceptsDocumentation = _dslSyntax.ConceptTypes
                .Select(concept => (concept.AssemblyQualifiedName, Type: Type.GetType(concept.AssemblyQualifiedName)))
                .GroupBy(concept => concept.Type.Assembly)
                .Select(group =>
                (
                    AssemblyDocumentation: LoadXmlDocumentForAssembly(group.Key.Location),
                    Concepts: group
                ))
                .SelectMany(group =>
                    group.Concepts.Select(concept =>
                    (
                        AssemblyQualifiedName: concept.AssemblyQualifiedName,
                        Documentation: GetConceptDocumentation(concept.Type, group.AssemblyDocumentation)
                    ))
                )
                .Where(concept => !concept.Documentation.IsEmpty())
                .OrderBy(concept => concept.AssemblyQualifiedName)
                .ToDictionary(concept => concept.AssemblyQualifiedName, concept => concept.Documentation);

            _dslDocumentationFile.Save(new DslDocumentation
            {
                Concepts = conceptsDocumentation
            });
        }

        public IEnumerable<string> Dependencies => null;

        private ConceptDocumentation GetConceptDocumentation(Type type, XDocument documentation)
        {
            try
            {
                var typeKey = $"T:{type.FullName}";
                var typeInfo = documentation
                    .Descendants(XName.Get("member"))
                    .SingleOrDefault(member => member.Attribute(XName.Get("name"))?.Value == typeKey);

                return new ConceptDocumentation
                {
                    Summary = GetValue(typeInfo, "summary"),
                    Remarks = GetValue(typeInfo, "remarks")
                };
            }
            catch (Exception e)
            {
                _logger.Warning($"Error getting summary from XML document for '{type.AssemblyQualifiedName}'. {e}");
                return new ConceptDocumentation();
            }
        }

        private static string GetValue(XElement typeInfo, string descendantName)
        {
            string value = typeInfo?.Descendants(XName.Get(descendantName)).SingleOrDefault()?.Value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private XDocument LoadXmlDocumentForAssembly(string codeBasePath)
        {
            var xDocument = new XDocument();
            try
            {
                var directory = Path.GetDirectoryName(codeBasePath);
                var filename = Path.GetFileNameWithoutExtension(codeBasePath);
                var xmlPath = Path.Combine(directory, $"{filename}.xml");
                
                if (File.Exists(xmlPath))
                {
                    xDocument = XDocument.Parse(File.ReadAllText(xmlPath));
                    _logger.Trace($"Loaded XML documentation from '{xmlPath}'.");
                }
                else
                {
                    _logger.Trace($"No XML documentation found at '{xmlPath}'.");
                }
            }
            catch (Exception e)
            {
                _logger.Warning($"Failed to load XML documentation for assembly '{codeBasePath}'. {e}");
            }

            return xDocument;
        }
    }
}