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
using Rhetos.Persistence;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptMapping))]
    public class ReferencePropertyEdmxCodeGenerator : ConceptMapping<ReferencePropertyInfo>
    {
        public override void GenerateCode(ReferencePropertyInfo referencePropertyInfo, ICodeBuilder codeBuilder)
        {
            if (referencePropertyInfo.DataStructure is IOrmDataStructure && referencePropertyInfo.Referenced is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(referencePropertyInfo.DataStructure));
                if (referencePropertyInfo.Referenced.GetKeyProperties().Equals(referencePropertyInfo.DataStructure.GetKeyProperties()))
                    codeBuilder.InsertCode(GetAssoctiationNodeForConceptualModelForSelfReference(referencePropertyInfo), EntityFrameworkMapping.ConceptualModelTag);
                else
                    codeBuilder.InsertCode(GetAssoctiationNodeForConceptualModel(referencePropertyInfo), EntityFrameworkMapping.ConceptualModelTag);
                codeBuilder.InsertCode(GetAssoctiationSetNodeForConceptualModel(referencePropertyInfo), EntityFrameworkMapping.ConceptualModelEntityContainerTag);
                codeBuilder.InsertCode(GetPropertyElementForConceptualModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypePropertyTag.Evaluate(referencePropertyInfo.DataStructure));

                codeBuilder.InsertCode(GetScalarPropertyElement(referencePropertyInfo), DataStructureEdmxCodeGenerator.EntitySetMappingPropertyTag.Evaluate(referencePropertyInfo.DataStructure));

                codeBuilder.InsertCode(GetPropertyElementForStorageModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.StorageModelEntityTypePropertyTag.Evaluate(referencePropertyInfo.DataStructure));
                if (referencePropertyInfo.Referenced.GetKeyProperties().Equals(referencePropertyInfo.DataStructure.GetKeyProperties()))
                {
                    codeBuilder.InsertCode(GetAssoctiationNodeForStorageModelForSelfReference(referencePropertyInfo), EntityFrameworkMapping.StorageModelTag);
                    codeBuilder.InsertCode(GetAssoctiationSetNodeForStorageModelForSelfeference(referencePropertyInfo), EntityFrameworkMapping.StorageModelEntityContainerTag);
                }
                else
                {
                    codeBuilder.InsertCode(GetAssoctiationNodeForStorageModel(referencePropertyInfo), EntityFrameworkMapping.StorageModelTag);
                    codeBuilder.InsertCode(GetAssoctiationSetNodeForStorageModel(referencePropertyInfo), EntityFrameworkMapping.StorageModelEntityContainerTag);
                }
            }
        }

        private static string GetAssoctiationNodeForStorageModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
  <Association Name=""{GetRefrenceName(referencePropertyInfo)}"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"" Multiplicity=""0..1"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}"" Multiplicity=""*"" />
    <ReferentialConstraint>
      <Principal Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}"">
        <PropertyRef Name=""{referencePropertyInfo.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForStorageModelForSelfReference(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
  <Association Name=""{GetRefrenceName(referencePropertyInfo)}"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"" Multiplicity=""0..1"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}Self"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}"" Multiplicity=""*"" />
    <ReferentialConstraint>
      <Principal Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.Referenced)}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{DataStructureEdmxCodeGenerator.GetName(referencePropertyInfo.DataStructure)}Self"">
        <PropertyRef Name=""{referencePropertyInfo.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForConceptualModel(ReferencePropertyInfo reference)
        {
            return $@"
  <Association Name=""{GetRefrenceName(reference)}"">
    <End Role=""{GetRefrenceName(reference)}_Source"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" Multiplicity=""*"" />
    <End Role=""{GetRefrenceName(reference)}_Target"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetRefrenceName(reference)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetRefrenceName(reference)}_Source"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForConceptualModelForSelfReference(ReferencePropertyInfo reference)
        {
            return $@"
  <Association Name=""{GetRefrenceName(reference)}"">
    <End Role=""{GetRefrenceName(reference)}_Source"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" Multiplicity=""*"" />
    <End Role=""{GetRefrenceName(reference)}_Target"" Type=""Self.{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetRefrenceName(reference)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetRefrenceName(reference)}_Source"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationSetNodeForConceptualModel(ReferencePropertyInfo reference)
        {
            return $@"
	<AssociationSet Name=""{GetRefrenceName(reference)}"" Association=""Self.{GetRefrenceName(reference)}"">
		<End Role=""{GetRefrenceName(reference)}_Source"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" />
		<End Role=""{GetRefrenceName(reference)}_Target"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" />
	</AssociationSet>";
        }

        private static string GetAssoctiationSetNodeForStorageModelForSelfeference(ReferencePropertyInfo reference)
        {
            return $@"
  <AssociationSet Name=""{GetRefrenceName(reference)}"" Association=""Self.{GetRefrenceName(reference)}"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}Self"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" />
  </AssociationSet>";
        }

        private static string GetAssoctiationSetNodeForStorageModel(ReferencePropertyInfo reference)
        {
            return $@"
  <AssociationSet Name=""{GetRefrenceName(reference)}"" Association=""Self.{GetRefrenceName(reference)}"">
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.Referenced)}"" />
    <End Role=""{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" EntitySet=""{DataStructureEdmxCodeGenerator.GetName(reference.DataStructure)}"" />
  </AssociationSet>";
        }

        private static string GetPropertyElementForStorageModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
    <Property Name=""{referencePropertyInfo.Name}ID"" Type=""uniqueidentifier"" customannotation:Index=""{{ Name: IX_{referencePropertyInfo.Name}ID, Order: 0 }}"" Nullable=""true"" />"; ;
        }

        private static string GetPropertyElementForConceptualModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
    <Property Name=""{referencePropertyInfo.Name}ID"" Type=""Guid"" />";
        }

        private static string GetScalarPropertyElement(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
        <ScalarProperty Name=""{referencePropertyInfo.Name}ID"" ColumnName=""{referencePropertyInfo.Name}ID"" />";
        }

        private static string GetNavigationPropertyNodeForConceptualModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"
    <NavigationProperty Name=""{referencePropertyInfo.Name}"" Relationship=""Self.{GetRefrenceName(referencePropertyInfo)}"" FromRole=""{GetRefrenceName(referencePropertyInfo)}_Source"" ToRole=""{GetRefrenceName(referencePropertyInfo)}_Target"" />";
        }

        public static string GetRefrenceName(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"{referencePropertyInfo.DataStructure.Module.Name}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}";
        }
    }
}
