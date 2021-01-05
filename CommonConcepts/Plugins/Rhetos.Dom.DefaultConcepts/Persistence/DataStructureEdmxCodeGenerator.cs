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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptMapping))]
    public class DataStructureEdmxCodeGenerator : ConceptMapping<DataStructureInfo>
    {
        public static readonly XmlTag<DataStructureInfo> ConceptualModelEntityTypePropertyTag = "ConceptualModelEntityTypeProperty";

        public static readonly XmlTag<DataStructureInfo> ConceptualModelEntityTypeNavigationPropertyTag = "ConceptualModelEntityTypeNavigationProperty";

        public static readonly XmlTag<DataStructureInfo> EntitySetMappingPropertyTag = "EntitySetMappingPropertyTag";

        public static readonly XmlTag<DataStructureInfo> StorageModelEntityTypePropertyTag = "StorageModelEntityTypeProperty";

        public static readonly XmlTag<DataStructureInfo> StorageModelCustomannotationIndexForIDTag = "StorageModelCustomannotationIndexForID";

        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public DataStructureEdmxCodeGenerator(RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        public override void GenerateCode(DataStructureInfo dataStructureInfo, ICodeBuilder codeBuilder)
        {
            if (dataStructureInfo is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetEntityTypeNodeForConceptualModel(dataStructureInfo), EntityFrameworkMapping.ConceptualModelTag);
                codeBuilder.InsertCode(GetEntitySetNodeForConceptualModel(dataStructureInfo), EntityFrameworkMapping.ConceptualModelEntityContainerTag);

                codeBuilder.InsertCode(GetEntitySetMappingForMapping(dataStructureInfo), EntityFrameworkMapping.MappingEntityContainerTag);

                codeBuilder.InsertCode(GetEntityTypeNodeForStorageModel(dataStructureInfo), EntityFrameworkMapping.StorageModelTag);
                codeBuilder.InsertCode(GetEntitySetNodeForStorageModel(dataStructureInfo), EntityFrameworkMapping.StorageModelEntityContainerTag);
            }
        }

        private string GetEntitySetMappingForMapping(DataStructureInfo dataStructureInfo)
        {
            return $@"
  <EntitySetMapping Name=""{GetName(dataStructureInfo)}"">
    <EntityTypeMapping TypeName=""{EntityFrameworkMapping.ConceptualModelNamespace}.{GetName(dataStructureInfo)}"">
      <MappingFragment StoreEntitySet=""{GetName(dataStructureInfo)}"">
        <ScalarProperty Name=""ID"" ColumnName=""ID"" />{EntitySetMappingPropertyTag.Evaluate(dataStructureInfo)}
      </MappingFragment>
    </EntityTypeMapping>
  </EntitySetMapping>";
        }

        private string GetEntityTypeNodeForStorageModel(DataStructureInfo dataStructureInfo)
        {
            return $@"
  <EntityType Name=""{GetName(dataStructureInfo)}"">
    <Key>
      <PropertyRef Name=""ID"" />
    </Key>
    <Property Name=""ID"" Type=""uniqueidentifier"" Nullable=""false"" {StorageModelCustomannotationIndexForIDTag.Evaluate(dataStructureInfo)}/>{StorageModelEntityTypePropertyTag.Evaluate(dataStructureInfo)}
  </EntityType>";
        }

        private string GetEntityTypeNodeForConceptualModel(DataStructureInfo dataStructureInfo)
        {
            var assemblyName = _rhetosBuildEnvironment.OutputAssemblyName;
            return $@"
  <EntityType Name=""{GetName(dataStructureInfo)}"" customannotation:ClrType=""Common.Queryable.{GetName(dataStructureInfo)}, {assemblyName}"">
    <Key>
      <PropertyRef Name=""ID"" />
    </Key>
    <Property Name=""ID"" Type=""Guid"" Nullable=""false"" />{ConceptualModelEntityTypePropertyTag.Evaluate(dataStructureInfo)}{ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(dataStructureInfo)}
  </EntityType>";
        }

        private string GetEntitySetNodeForConceptualModel(DataStructureInfo dataStructureInfo)
        {
            return $@"
    <EntitySet Name=""{GetName(dataStructureInfo)}"" EntityType=""Self.{GetName(dataStructureInfo)}"" />";
        }

        private string GetEntitySetNodeForStorageModel(DataStructureInfo dataStructureInfo)
        {
            var ormStructure = (IOrmDataStructure)dataStructureInfo;
            return $@"
    <EntitySet Name=""{GetName(dataStructureInfo)}"" EntityType=""Self.{GetName(dataStructureInfo)}"" Schema=""{ormStructure.GetOrmSchema()}"" Table=""{ormStructure.GetOrmDatabaseObject()}"" />";
        }

        public static string GetName(DataStructureInfo dataStructureInfo)
        {
            return $@"{dataStructureInfo.Module.Name}_{dataStructureInfo.Name}";
        }
    }
}
