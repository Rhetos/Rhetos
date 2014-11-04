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
    public class PolymorphicInfo : DataStructureInfo, IOrmDataStructure, IMacroConcept, IAlternativeInitializationConcept
    {
        public static readonly SqlTag<PolymorphicInfo> PolymorphicPropertyNameTag = new SqlTag<PolymorphicInfo>("PolymorphicPropertyName");
        public static readonly SqlTag<PolymorphicInfo> PolymorphicPropertyInitializationTag = new SqlTag<PolymorphicInfo>("PolymorphicPropertyInitialization");
        public static readonly SqlTag<PolymorphicInfo> SubtypeQueryTag = new SqlTag<PolymorphicInfo>("SubtypeQuery");

        //===========================================================
        // Creating SQL view - union of subtypes:

        public SqlObjectInfo Dependency_View { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_View" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_View = new SqlObjectInfo
            {
                Module = Module,
                Name = Name,
                CreateSql = CreateViewCodeSnippet(),
                RemoveSql = RemoveViewCodeSnippet()
            };

            createdConcepts = new[] { Dependency_View };
        }

        private string CreateViewCodeSnippet()
        {
            // Column names list (@columnList) is separated from the create query (@sql)
            // to be used in subqueryes, to make sure that the order of columns is the same
            // in all the subqueries. This is necessary for UNION ALL.

            return string.Format(
@"
DECLARE @columnList NVARCHAR(MAX);
SET @columnList = N'{2}';

DECLARE @sql NVARCHAR(MAX);
SET @sql = N'CREATE VIEW {0}.{1}
AS
SELECT
    ID = CONVERT(UNIQUEIDENTIFIER, NULL){3}
WHERE
    0=1
{4}';


PRINT @sql;
EXEC (@sql);
",
                Module.Name,
                Name,
                PolymorphicPropertyNameTag.Evaluate(this),
                PolymorphicPropertyInitializationTag.Evaluate(this),
                SubtypeQueryTag.Evaluate(this));
        }

        private string RemoveViewCodeSnippet()
        {
            return string.Format("DROP VIEW {0}.{1};", Module.Name, Name);
        }

        //===========================================================

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var subtypeString = new ShortStringPropertyInfo { DataStructure = this, Name = "Subtype" };
            newConcepts.Add(subtypeString);
            newConcepts.Add(new PolymorphicPropertyInfo { Property = subtypeString });

            var existingPolymorphicProperties = new HashSet<string>(
                existingConcepts.OfType<PolymorphicPropertyInfo>()
                    .Where(pp => pp.Property.DataStructure == this)
                    .Select(pp => pp.Property.Name));

            newConcepts.AddRange(existingConcepts
                .OfType<PropertyInfo>()
                .Where(p => p.DataStructure == this)
                .Where(p => !existingPolymorphicProperties.Contains(p.Name))
                .Select(p => new PolymorphicPropertyInfo { Property = p }));

            // Automatically materialize the polymorphic entity if it is referenced or extended, so the polymorphic can be used in FK constraint.
            if (existingConcepts.OfType<ReferencePropertyInfo>().Where(r => r.Referenced == this && r.DataStructure is EntityInfo).Any()
                || existingConcepts.OfType<DataStructureExtendsInfo>().Where(e => e.Base == this && e.Extension is EntityInfo).Any())
                newConcepts.Add(new PolymorphicMaterializedInfo { Polymorphic = this });

            return newConcepts;
        }

        public string GetOrmSchema()
        {
            return Dependency_View.Module.Name;
        }

        public string GetOrmDatabaseObject()
        {
            return Dependency_View.Name;
        }
    }
}
