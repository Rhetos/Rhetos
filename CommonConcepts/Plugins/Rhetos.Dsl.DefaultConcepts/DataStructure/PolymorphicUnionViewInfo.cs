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
    public class PolymorphicUnionViewInfo : SqlObjectInfo
    {
        public static readonly SqlTag<PolymorphicUnionViewInfo> PolymorphicPropertyNameTag = "PolymorphicPropertyName";
        public static readonly SqlTag<PolymorphicUnionViewInfo> PolymorphicPropertyInitializationTag = "PolymorphicPropertyInitialization";
        public static readonly SqlTag<PolymorphicUnionViewInfo> SubtypeQueryTag = "SubtypeQuery";

        public PolymorphicUnionViewInfo()
        {
        }

        public PolymorphicUnionViewInfo(PolymorphicInfo conceptInfo)
        {
            this.Module = conceptInfo.Module;
            this.Name = conceptInfo.Name;
            this.CreateSql = CreateViewCodeSnippet(conceptInfo);
            this.RemoveSql = RemoveViewCodeSnippet(conceptInfo);
        }

        public string CreateViewCodeSnippet(PolymorphicInfo conceptInfo)
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
                conceptInfo.Module.Name,
                conceptInfo.Name,
                PolymorphicPropertyNameTag.Evaluate(this),
                PolymorphicPropertyInitializationTag.Evaluate(this),
                SubtypeQueryTag.Evaluate(this));
        }

        public string RemoveViewCodeSnippet(PolymorphicInfo conceptInfo)
        {
            return string.Format("DROP VIEW {0}.{1};", conceptInfo.Module.Name, conceptInfo.Name);
        }
    }
}
