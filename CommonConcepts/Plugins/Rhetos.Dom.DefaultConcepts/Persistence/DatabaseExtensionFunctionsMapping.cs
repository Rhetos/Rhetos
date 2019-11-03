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
using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    [Export(typeof(IConceptMapping))]
    public class DatabaseExtensionFunctionsMapping : ConceptMapping<InitializationConcept>
    {
        public override void GenerateCode(InitializationConcept conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(
@"  <Function Name=""StringEqualsCaseInsensitive"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a = b</DefiningExpression>
  </Function>
  <Function Name=""StringNotEqualsCaseInsensitive"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a != b</DefiningExpression>
  </Function>
  <Function Name=""StringIsLessThen"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a &lt; b</DefiningExpression>
  </Function>
  <Function Name=""StringIsLessThenOrEqual"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a &lt;= b</DefiningExpression>
  </Function>
  <Function Name=""StringIsGreaterThen"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a &gt; b</DefiningExpression>
  </Function>
  <Function Name=""StringIsGreaterThenOrEqual"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a &gt;= b</DefiningExpression>
  </Function>
  <Function Name=""IntStartsWith"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.Int32"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>CAST(a as Edm.String) LIKE b + '%'</DefiningExpression>
  </Function>
  <Function Name=""StringStartsWithCaseInsensitive"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a LIKE b + '%'</DefiningExpression>
  </Function>
  <Function Name=""StringEndsWithCaseInsensitive"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a LIKE '%' + b</DefiningExpression>
  </Function>
  <Function Name=""StringContainsCaseInsensitive"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.String"" />
    <Parameter Name=""b"" Type=""Edm.String"" />
    <DefiningExpression>a LIKE '%' + b + '%'</DefiningExpression>
  </Function>
  <Function Name=""StringLike"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""text"" Type=""Edm.String"" />
    <Parameter Name=""pattern"" Type=""Edm.String"" />
    <DefiningExpression>text LIKE pattern</DefiningExpression>
  </Function>
  <Function Name=""IntCastToString"" ReturnType=""Edm.String"">
    <Parameter Name=""a"" Type=""Edm.Int32"" />
    <DefiningExpression>CAST(a as Edm.String)</DefiningExpression>
  </Function>
  <Function Name=""GuidIsGreaterThan"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.Guid"" />
    <Parameter Name=""b"" Type=""Edm.Guid"" />
    <DefiningExpression>a &gt; b</DefiningExpression>
  </Function>
  <Function Name=""GuidIsGreaterThanOrEqual"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.Guid"" />
    <Parameter Name=""b"" Type=""Edm.Guid"" />
    <DefiningExpression>a &gt;= b</DefiningExpression>
  </Function>
  <Function Name=""GuidIsLessThan"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.Guid"" />
    <Parameter Name=""b"" Type=""Edm.Guid"" />
    <DefiningExpression>a &lt; b</DefiningExpression>
  </Function>
  <Function Name=""GuidIsLessThanOrEqual"" ReturnType=""Edm.Boolean"">
    <Parameter Name=""a"" Type=""Edm.Guid"" />
    <Parameter Name=""b"" Type=""Edm.Guid"" />
    <DefiningExpression>a &lt;= b</DefiningExpression>
  </Function>
", EntityFrameworkMapping.ConceptualModelTag);

            codeBuilder.InsertCode(
$@"  <Function Name=""{DatabaseExtensionFunctions.InterceptFullTextSearchFunction}"" ReturnType=""bit"" >
    <Parameter Name = ""itemId"" Mode = ""In"" Type = ""uniqueidentifier"" />
    <Parameter Name = ""pattern"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""tableName"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""searchColumns"" Mode = ""In"" Type = ""nvarchar"" />
  </Function>
  <Function Name=""{DatabaseExtensionFunctions.InterceptFullTextSearchFunction}"" ReturnType=""bit"" >
    <Parameter Name = ""itemId"" Mode = ""In"" Type = ""uniqueidentifier"" />
    <Parameter Name = ""pattern"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""tableName"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""searchColumns"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""rankTop"" Mode = ""In"" Type = ""int"" />
  </Function>
  <Function Name=""{DatabaseExtensionFunctions.InterceptFullTextSearchFunction}"" ReturnType=""bit"" >
    <Parameter Name = ""itemId"" Mode = ""In"" Type = ""int"" />
    <Parameter Name = ""pattern"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""tableName"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""searchColumns"" Mode = ""In"" Type = ""nvarchar"" />
  </Function>
  <Function Name=""{DatabaseExtensionFunctions.InterceptFullTextSearchFunction}"" ReturnType=""bit"" >
    <Parameter Name = ""itemId"" Mode = ""In"" Type = ""int"" />
    <Parameter Name = ""pattern"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""tableName"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""searchColumns"" Mode = ""In"" Type = ""nvarchar"" />
    <Parameter Name = ""rankTop"" Mode = ""In"" Type = ""int"" />
  </Function>
  <Function Name=""{EFExpression.ContainsIdsFunction}"" ReturnType=""bit"" >
    <Parameter Name = ""id"" Mode = ""In"" Type = ""uniqueidentifier"" />
    <Parameter Name = ""concatenatedids"" Mode = ""In"" Type = ""nvarchar"" />
  </Function>
", EntityFrameworkMapping.StorageModelTag);
        }
    }
}
