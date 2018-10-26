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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Persistence;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IEdmxCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureEdmxCodeGenerator : IEdmxCodeGenerator
    {
        public static readonly XmlTag<DataStructureInfo> ConceptualModelEntityTypePropertyTag = "ConceptualModelEntityTypeProperty";

        public static readonly XmlTag<DataStructureInfo> ConceptualModelEntityTypeNavigationPropertyTag = "ConceptualModelEntityTypeNavigationProperty";

        public static readonly XmlTag<DataStructureInfo> EntitySetMappingPropertyTag = "EntitySetMappingPropertyTag";

        public static readonly XmlTag<DataStructureInfo> StorageModelEntityTypePropertyTag = "StorageModelEntityTypeProperty";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var dataStructureInfo = conceptInfo as DataStructureInfo;

            if (dataStructureInfo is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetEntityTypeNodeForConceptualModel(dataStructureInfo), EdmxInitialCodeSnippet.ConceptualModeEntityTypeTag);
                codeBuilder.InsertCode(GetEntitySetNodeForConceptualModel(dataStructureInfo), EdmxInitialCodeSnippet.ConceptualModeEntitySetTag);

                codeBuilder.InsertCode(GetEntitySetMappingForMapping(dataStructureInfo), EdmxInitialCodeSnippet.MappingTag);

                codeBuilder.InsertCode(GetEntityTypeNodeForStorageModel(dataStructureInfo), EdmxInitialCodeSnippet.StorageModeEntityTypeTag);
                codeBuilder.InsertCode(GetEntitySetNodeForStorageModel(dataStructureInfo), EdmxInitialCodeSnippet.StorageModeEntitySetTag);
            }
        }

        private static string GetEntitySetMappingForMapping(DataStructureInfo dataStructureInfo)
        {
            return "\n" + $@"  <EntitySetMapping Name=""{GetEFName(dataStructureInfo)}"">
    <EntityTypeMapping TypeName=""Common.{GetEFName(dataStructureInfo)}"">
      <MappingFragment StoreEntitySet=""{GetEFName(dataStructureInfo)}"">
        <ScalarProperty Name=""ID"" ColumnName=""ID"" />{EntitySetMappingPropertyTag.Evaluate(dataStructureInfo)}
      </MappingFragment>
    </EntityTypeMapping>
  </EntitySetMapping>";
        }

        private static string GetEntityTypeNodeForStorageModel(DataStructureInfo dataStructureInfo)
        {
            return "\n" + $@"  <EntityType Name=""{dataStructureInfo.Module.Name}_{dataStructureInfo.Name}"">
    <Key>
      <PropertyRef Name=""ID"" />
    </Key>
    <Property Name=""ID"" Type=""uniqueidentifier"" Nullable=""false"" />{StorageModelEntityTypePropertyTag.Evaluate(dataStructureInfo)}
  </EntityType>";
        }

        private static string GetEntityTypeNodeForConceptualModel(DataStructureInfo dataStructureInfo)
        {
            return "\n" + $@"  <EntityType Name=""{GetEFName(dataStructureInfo)}"" customannotation:ClrType=""Common.Queryable.{GetEFName(dataStructureInfo)}, ServerDom.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"">
    <Key>
      <PropertyRef Name=""ID"" />
    </Key>
    <Property Name=""ID"" Type=""Guid"" Nullable=""false"" />{ConceptualModelEntityTypePropertyTag.Evaluate(dataStructureInfo)}{ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(dataStructureInfo)}
  </EntityType>";
        }

        private static string GetEntitySetNodeForConceptualModel(DataStructureInfo dataStructureInfo)
        {
            return "\n" + $@"    <EntitySet Name=""{GetEFName(dataStructureInfo)}"" EntityType=""Self.{GetEFName(dataStructureInfo)}"" />";
        }

        private static string GetEntitySetNodeForStorageModel(DataStructureInfo dataStructureInfo)
        {
            return "\n" + $@"    <EntitySet Name=""{GetEFName(dataStructureInfo)}"" EntityType=""Self.{GetEFName(dataStructureInfo)}"" Schema=""{dataStructureInfo.Module.Name}"" Table=""{dataStructureInfo.Name}"" />";
        }

        private static string GetEFName(DataStructureInfo dataStructureInfo)
        {
            return $@"{dataStructureInfo.Module}_{dataStructureInfo.Name}";
        }
    }
}
