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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlDependsOnPropertyInfo))]
    public class SqlDependsOnPropertyDatabaseDefinition : IConceptDatabaseDefinitionExtension
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly Lazy<MultiDictionary<(string, string, string), UniqueMultiplePropertiesInfo>> _uniqueIndexesByFirstProperty;

        public SqlDependsOnPropertyDatabaseDefinition(IDslModel dslModel)
        {
            _uniqueIndexesByFirstProperty = new Lazy<MultiDictionary<(string, string, string), UniqueMultiplePropertiesInfo>>(
                () => dslModel.FindByType<UniqueMultiplePropertiesInfo>()
                    .Where(unique => unique.Dependency_SqlIndex.SqlImplementation())
                    .ToMultiDictionary(GetFirstProperty, unique => unique));
        }

        private (string, string, string) GetFirstProperty(UniqueMultiplePropertiesInfo unique)
        {
            int separator = unique.PropertyNames.IndexOf(' ');
            string firstPropertyName = separator >= 0
                ? unique.PropertyNames.Substring(0, separator)
                : unique.PropertyNames;
            return (unique.DataStructure.Module.Name, unique.DataStructure.Name, firstPropertyName);
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo) => "";

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) => "";

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (SqlDependsOnPropertyInfo)conceptInfo;

            var newDependencies = new List<Tuple<IConceptInfo, IConceptInfo>>(1);
            AddDependencies(newDependencies, info.DependsOn, info.Dependent);
            createdDependencies = newDependencies;
        }

        public void AddDependencies(List<Tuple<IConceptInfo, IConceptInfo>> newDependencies, PropertyInfo property, IConceptInfo dependent)
        {
            newDependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(property, dependent));

            var propertyKey = (property.DataStructure.Module.Name, property.DataStructure.Name, property.Name);
            if (_uniqueIndexesByFirstProperty.Value.TryGetValue(propertyKey, out var indexes))
                newDependencies.AddRange(
                    indexes.Select(unique => Tuple.Create<IConceptInfo, IConceptInfo>(unique.Dependency_SqlIndex, dependent)));
        }
    }
}
