﻿/*
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

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlDependsOnDataStructureInfo))]
    public class SqlDependsOnDataStructureDatabaseDefinition : IConceptDatabaseDefinitionExtension
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly Lazy<MultiDictionary<(string, string), PropertyInfo>> _propertiesByDataStructure;
        private readonly SqlDependsOnPropertyDatabaseDefinition _propertyDependencies;

        public SqlDependsOnDataStructureDatabaseDefinition(IDslModel dslModel)
        {
            _propertiesByDataStructure = new Lazy<MultiDictionary<(string, string), PropertyInfo>>(
                () => dslModel.FindByType<PropertyInfo>()
                    .ToMultiDictionary(p => (p.DataStructure.Module.Name, p.DataStructure.Name), p => p));
            _propertyDependencies = new SqlDependsOnPropertyDatabaseDefinition(dslModel);
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo) => "";

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) => "";

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (SqlDependsOnDataStructureInfo)conceptInfo;

            _propertiesByDataStructure.Value.TryGetValue(
                (info.DependsOn.Module.Name, info.DependsOn.Name),
                out List<PropertyInfo> properties);

            var newDependencies = new List<Tuple<IConceptInfo, IConceptInfo>>(1 + (properties?.Count ?? 0));

            newDependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(info.DependsOn, info.Dependent));

            if (properties != null)
                foreach (var property in properties)
                    if (property != info.Dependent)
                        _propertyDependencies.AddDependencies(newDependencies, property, info.Dependent);

            createdDependencies = newDependencies;
        }
    }
}
