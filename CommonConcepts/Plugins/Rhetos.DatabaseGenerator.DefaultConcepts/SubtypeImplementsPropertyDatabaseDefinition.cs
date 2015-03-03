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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SubtypeImplementsPropertyInfo))]
    public class SubtypeImplementsPropertyDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        ConceptMetadata _conceptMetadata;

        public SubtypeImplementsPropertyDatabaseDefinition(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (SubtypeImplementsPropertyInfo)conceptInfo;

            codeBuilder.InsertCode(PropertyImplementationSnippet(info),
                ExtensibleSubtypeSqlViewInfo.PropertyImplementationTag,
                (ExtensibleSubtypeSqlViewInfo)info.Dependency_ImplementationView);

            createdDependencies = null;
        }

        private string PropertyImplementationSnippet(SubtypeImplementsPropertyInfo info)
        {
            var propertyColumnNames = _conceptMetadata.Get(info.Property, PropertyDatabaseDefinition.ColumnNamesMetadata);

            return ",\r\n    " + propertyColumnNames.Single() + " = " + info.Expression;
        }
    }
}