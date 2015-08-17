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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Polymorphic")]
    public class PolymorphicInfo : DataStructureInfo, IOrmDataStructure
    {
        public PolymorphicUnionViewInfo GetUnionViewPrototype()
        {
            return new PolymorphicUnionViewInfo { Module = Module, Name = Name };
        }

        public string GetOrmSchema()
        {
            return Module.Name; // ORM will be mapped to PolymorphicUnionViewInfo from the database.
        }

        public string GetOrmDatabaseObject()
        {
            return Name; // ORM will be mapped to PolymorphicUnionViewInfo from the database.
        }
    }

    [Export(typeof(IConceptMacro))]
    public class PolymorphicMacro : IConceptMacro<PolymorphicInfo>
    {
        public static readonly CsTag<PolymorphicInfo> SetFilterExpressionTag = "SetFilterExpression";

        public IEnumerable<IConceptInfo> CreateNewConcepts(PolymorphicInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Create a supertype SQL view - union of subtypes:

            newConcepts.Add(new PolymorphicUnionViewInfo(conceptInfo));

            // Create a subtype name property:

            var subtypeString = new ShortStringPropertyInfo { DataStructure = conceptInfo, Name = "Subtype" };
            newConcepts.Add(subtypeString);
            newConcepts.Add(new PolymorphicPropertyInfo { Property = subtypeString });

            // Mark polymorphic properties:

            var existingPolymorphicProperties = new HashSet<string>(
                existingConcepts.FindByType<PolymorphicPropertyInfo>()
                    .Where(pp => pp.Property.DataStructure == conceptInfo)
                    .Select(pp => pp.Property.Name));

            newConcepts.AddRange(existingConcepts
                .FindByType<PropertyInfo>()
                .Where(p => p.DataStructure == conceptInfo)
                .Where(p => !existingPolymorphicProperties.Contains(p.Name))
                .Select(p => new PolymorphicPropertyInfo { Property = p }));

            // Automatically materialize the polymorphic entity if it is referenced or extended, so the polymorphic can be used in FK constraint:

            if (existingConcepts.FindByType<ReferencePropertyInfo>().Where(r => r.Referenced == conceptInfo && r.DataStructure is EntityInfo).Any()
                || existingConcepts.FindByType<DataStructureExtendsInfo>().Where(e => e.Base == conceptInfo && e.Extension is EntityInfo).Any())
                newConcepts.Add(new PolymorphicMaterializedInfo { Polymorphic = conceptInfo });

            // Optimized filter by subtype allows better SQL server optimization of the execution plan:

            newConcepts.Add(new FilterByInfo
            {
                Source = conceptInfo,
                Parameter = "Rhetos.Dom.DefaultConcepts.FilterSubtype",
                Expression = @"(repository, parameter) =>
                {{
                    Expression<Func<Common.Queryable." + conceptInfo.Module.Name + @"_" + conceptInfo.Name + @", bool>> filterExpression = null;
                    parameter.ImplementationName = parameter.ImplementationName ?? """";
                    " + SetFilterExpressionTag.Evaluate(conceptInfo) + @"
                    if (filterExpression == null)
                        throw new Rhetos.ClientException(string.Format(""Invalid subtype name or implementation name provided: '{0}', '{1}'."",
                            parameter.Subtype, parameter.ImplementationName));
                    return Filter(Query().Where(filterExpression), parameter.Ids).ToItems().ToArray();
                }}"
            });

            return newConcepts;
        }
    }
}
