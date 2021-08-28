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
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptMapping))]
    public class UniqueReferenceEdmxCodeGenerator : ConceptMapping<UniqueReferenceInfo>
    {
        public override void GenerateCode(UniqueReferenceInfo uniqueReferenceInfo, ICodeBuilder codeBuilder)
        {
            if (uniqueReferenceInfo.Base is IOrmDataStructure && uniqueReferenceInfo.Extension is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModelForExtension(uniqueReferenceInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(uniqueReferenceInfo.Base));
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModel(uniqueReferenceInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(uniqueReferenceInfo.Extension));
                codeBuilder.InsertCode(GetAssociationNodeForConceptualModel(uniqueReferenceInfo), EntityFrameworkMapping.ConceptualModelTag);

                codeBuilder.InsertCode(GetAssociationSetNodeForConceptualModel(uniqueReferenceInfo), EntityFrameworkMapping.ConceptualModelEntityContainerTag);

                codeBuilder.InsertCode(GetAssociationNodeForStorageModel(uniqueReferenceInfo), EntityFrameworkMapping.StorageModelTag);
                codeBuilder.InsertCode(GetAttributeForIDPropertyForStorageModel(), DataStructureEdmxCodeGenerator.StorageModelCustomannotationIndexForIDTag.Evaluate(uniqueReferenceInfo.Extension));
                codeBuilder.InsertCode(GetAssociationSetNodeForStorageModel(uniqueReferenceInfo), EntityFrameworkMapping.StorageModelEntityContainerTag);
            }
        }

        private static string GetAssociationSetNodeForStorageModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
  <AssociationSet Name=""{GetAssociationSetName(uniqueReferenceInfo)}"" Association=""Self.{GetAssociationSetName(uniqueReferenceInfo)}"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" />
  </AssociationSet>";
        }

        private static string GetAssociationSetNodeForConceptualModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
	<AssociationSet Name=""{GetAssociationSetName(uniqueReferenceInfo)}"" Association=""Self.{GetAssociationSetName(uniqueReferenceInfo)}"">
		<End Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Source"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" />
		<End Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Target"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" />
	</AssociationSet>";
        }

        private static string GetAssociationNodeForStorageModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
  <Association Name=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" Multiplicity=""1"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"">
        <PropertyRef Name=""ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssociationNodeForConceptualModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
  <Association Name=""{GetAssociationSetName(uniqueReferenceInfo)}"">
    <End Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Source"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}"" Multiplicity=""0..1"" />
    <End Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Target"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Base)}"" Multiplicity=""1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetAssociationSetName(uniqueReferenceInfo)}_Source"">
        <PropertyRef Name=""ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAttributeForIDPropertyForStorageModel()
        {
            return $@" customannotation:Index=""{{ Name: IX_ID, Order: 0 }}""";
        }

        private static string GetNavigationPropertyNodeForConceptualModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
    <NavigationProperty Name=""Base"" Relationship=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base"" FromRole=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base_Source"" ToRole=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base_Target"" />";
        }

        private static string GetNavigationPropertyNodeForConceptualModelForExtension(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"
    <NavigationProperty Name=""{GetNavigationPropertyNameForConceptualModelForExtension(uniqueReferenceInfo)}"" Relationship=""Self.{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base"" FromRole=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base_Target"" ToRole=""{DataStructureEdmxCodeGenerator.GetName(uniqueReferenceInfo.Extension)}_Base_Source"" />";
        }

        private static string GetNavigationPropertyNameForConceptualModelForExtension(UniqueReferenceInfo uniqueReferenceInfo)
        {
            if (uniqueReferenceInfo.Base.Module.Name.Equals(uniqueReferenceInfo.Extension.Module.Name))
                return $@"Extension_{uniqueReferenceInfo.Extension.Name}";
            else
                return $@"Extension_{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}";
        }

        private static string GetAssociationSetName(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return $@"{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}_Base";
        }
    }
}
