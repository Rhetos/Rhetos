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
using Rhetos.Dsl.DefaultConcepts.DatabaseWorkarounds;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete IConceptDatabaseDefinition
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(IncludeInfo))]
    public class IncludeDatabaseDefinition : IConceptDatabaseDefinitionExtension
#pragma warning restore CS0618 // Type or member is obsolete IConceptDatabaseDefinition
    {
        private readonly ConceptMetadata _conceptMetadata;
        private readonly IDslModel _dslModel;

        public IncludeDatabaseDefinition(ConceptMetadata conceptMetadata, IDslModel dslModel)
        {
            _conceptMetadata = conceptMetadata;
            _dslModel = dslModel;
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
            var info = (IncludeInfo)conceptInfo;
            var newDependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();

            if (info.SqlIndex.SqlImplementation())
            {
                var names = info.Columns.Split(' ');
                for (int i = 0; i < names.Length; i++)
                {
                    var property = (PropertyInfo)_dslModel.FindByKey($"PropertyInfo {info.SqlIndex.DataStructure.FullName}.{names[i]}");

                    // Trying to recognize a column, even if not specified by property name, in order to automatically add a dependency if possible.
                    if (property == null && names[i].EndsWith("ID", StringComparison.OrdinalIgnoreCase) && names[i].Length > 2)
                        property = _dslModel.FindByKey($"PropertyInfo {info.SqlIndex.DataStructure.FullName}.{names[i][0..^2]}") as ReferencePropertyInfo; // Only if the property is ReferencePropertyInfo.

                    if (property != null)
                    {
                        names[i] = _conceptMetadata.GetColumnName(property) ?? names[i];
                        newDependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(property, info.SqlIndex));
                    }
                }

                codeBuilder.InsertCode(string.Join(", ", names), SqlIndexMultipleDatabaseDefinition.IncludeTag, info.SqlIndex);
            }

            createdDependencies = newDependencies;
        }
    }
}
