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
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureEdmxCodeGenerator))]
    public class ReferencePropertyEdmxCodeGenerator : IEdmxCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var referencePropertyInfo = conceptInfo as ReferencePropertyInfo;

            if (referencePropertyInfo.DataStructure is IOrmDataStructure && referencePropertyInfo.Referenced is IOrmDataStructure)
            {
                codeBuilder.InsertCode("\n" + GetNavigationPropertyNodeForConceptualModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(referencePropertyInfo.DataStructure));
                if (referencePropertyInfo.Referenced.GetKeyProperties().Equals(referencePropertyInfo.DataStructure.GetKeyProperties()))
                    codeBuilder.InsertCode(GetAssoctiationNodeForConceptualModelForSelfReference(referencePropertyInfo), EdmxInitialCodeSnippet.ConceptualModelAssociationTag);
                else
                    codeBuilder.InsertCode(GetAssoctiationNodeForConceptualModel(referencePropertyInfo), EdmxInitialCodeSnippet.ConceptualModelAssociationTag);
                codeBuilder.InsertCode(GetAssoctiationSetNodeForConceptualModel(referencePropertyInfo), EdmxInitialCodeSnippet.ConceptualModelAssociationSetTag);
                codeBuilder.InsertCode("\n" + GetPropertyElementForConceptualModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypePropertyTag.Evaluate(referencePropertyInfo.DataStructure));

                codeBuilder.InsertCode("\n" + GetScalarPropertyElement(referencePropertyInfo), DataStructureEdmxCodeGenerator.EntitySetMappingPropertyTag.Evaluate(referencePropertyInfo.DataStructure));

                codeBuilder.InsertCode("\n" + GetPropertyElementForStorageModel(referencePropertyInfo), DataStructureEdmxCodeGenerator.StorageModelEntityTypePropertyTag.Evaluate(referencePropertyInfo.DataStructure));
                if (referencePropertyInfo.Referenced.GetKeyProperties().Equals(referencePropertyInfo.DataStructure.GetKeyProperties()))
                {
                    codeBuilder.InsertCode(GetAssoctiationNodeForStorageModelForSelfReference(referencePropertyInfo), EdmxInitialCodeSnippet.StorageModelAssociationTag);
                    codeBuilder.InsertCode(GetAssoctiationSetNodeForStorageModelForSelfeference(referencePropertyInfo), EdmxInitialCodeSnippet.StorageModelAssociationSetTag);
                }
                else
                {
                    codeBuilder.InsertCode(GetAssoctiationNodeForStorageModel(referencePropertyInfo), EdmxInitialCodeSnippet.StorageModelAssociationTag);
                    codeBuilder.InsertCode(GetAssoctiationSetNodeForStorageModel(referencePropertyInfo), EdmxInitialCodeSnippet.StorageModelAssociationSetTag);
                }
            }
        }

        private static string GetAssoctiationNodeForStorageModel(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <Association Name=""{reference.DataStructure.Module}_{reference.DataStructure.Name}_{reference.Name}"">
    <End Role=""{reference.Referenced.Module}_{reference.Referenced.Name}"" Type=""Self.{reference.Referenced.Module}_{reference.Referenced.Name}"" Multiplicity=""0..1"" />
    <End Role=""{reference.DataStructure.Module}_{reference.DataStructure.Name}"" Type=""Self.{reference.DataStructure.Module}_{reference.DataStructure.Name}"" Multiplicity=""*"" />
    <ReferentialConstraint>
      <Principal Role=""{reference.Referenced.Module}_{reference.Referenced.Name}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{reference.DataStructure.Module}_{reference.DataStructure.Name}"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForStorageModelForSelfReference(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <Association Name=""{reference.DataStructure.Module}_{reference.DataStructure.Name}_{reference.Name}"">
    <End Role=""{reference.Referenced.Module}_{reference.Referenced.Name}"" Type=""Self.{reference.Referenced.Module}_{reference.Referenced.Name}"" Multiplicity=""0..1"" />
    <End Role=""{reference.DataStructure.Module}_{reference.DataStructure.Name}Self"" Type=""Self.{reference.DataStructure.Module}_{reference.DataStructure.Name}"" Multiplicity=""*"" />
    <ReferentialConstraint>
      <Principal Role=""{reference.Referenced.Module}_{reference.Referenced.Name}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{reference.DataStructure.Module}_{reference.DataStructure.Name}Self"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForConceptualModel(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <Association Name=""{GetAssociationSetNameForRefrence(reference)}"">
    <End Role=""{GetAssociationSetNameForRefrence(reference)}_Source"" Type=""Self.{GetEFName(reference.DataStructure)}"" Multiplicity=""*"" />
    <End Role=""{GetAssociationSetNameForRefrence(reference)}_Target"" Type=""Self.{GetEFName(reference.Referenced)}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetAssociationSetNameForRefrence(reference)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetAssociationSetNameForRefrence(reference)}_Source"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationNodeForConceptualModelForSelfReference(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <Association Name=""{GetAssociationSetNameForRefrence(reference)}"">
    <End Role=""{GetAssociationSetNameForRefrence(reference)}_Source"" Type=""Self.{GetEFName(reference.DataStructure)}"" Multiplicity=""*"" />
    <End Role=""{GetAssociationSetNameForRefrence(reference)}_Target"" Type=""Self.{GetEFName(reference.Referenced)}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetAssociationSetNameForRefrence(reference)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetAssociationSetNameForRefrence(reference)}_Source"">
        <PropertyRef Name=""{reference.Name}ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssoctiationSetNodeForConceptualModel(ReferencePropertyInfo reference)
        {
            return "\n" + $@"	<AssociationSet Name=""{GetAssociationSetNameForRefrence(reference)}"" Association=""Self.{GetAssociationSetNameForRefrence(reference)}"">
		<End Role=""{GetAssociationSetNameForRefrence(reference)}_Source"" EntitySet=""{GetEFName(reference.DataStructure)}"" />
		<End Role=""{GetAssociationSetNameForRefrence(reference)}_Target"" EntitySet=""{GetEFName(reference.Referenced)}"" />
	</AssociationSet>";
        }

        private static string GetAssoctiationSetNodeForStorageModelForSelfeference(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <AssociationSet Name=""{GetAssociationSetNameForRefrence(reference)}"" Association=""Self.{GetAssociationSetNameForRefrence(reference)}"">
    <End Role=""{GetEFName(reference.Referenced)}"" EntitySet=""{GetEFName(reference.Referenced)}"" />
    <End Role=""{GetEFName(reference.DataStructure)}Self"" EntitySet=""{GetEFName(reference.DataStructure)}"" />
  </AssociationSet>";
        }

        private static string GetAssoctiationSetNodeForStorageModel(ReferencePropertyInfo reference)
        {
            return "\n" + $@"  <AssociationSet Name=""{GetAssociationSetNameForRefrence(reference)}"" Association=""Self.{GetAssociationSetNameForRefrence(reference)}"">
    <End Role=""{GetEFName(reference.Referenced)}"" EntitySet=""{GetEFName(reference.Referenced)}"" />
    <End Role=""{GetEFName(reference.DataStructure)}"" EntitySet=""{GetEFName(reference.DataStructure)}"" />
  </AssociationSet>";
        }

        private static string GetPropertyElementForStorageModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"    <Property Name=""{referencePropertyInfo.Name}ID"" Type=""uniqueidentifier"" customannotation:Index=""{{ Name: IX_{referencePropertyInfo.Name}ID, Order: 0 }}"" Nullable=""true"" />"; ;
        }

        private static string GetPropertyElementForConceptualModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"    <Property Name=""{referencePropertyInfo.Name}ID"" Type=""Guid"" />";
        }

        private static string GetScalarPropertyElement(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"        <ScalarProperty Name=""{referencePropertyInfo.Name}ID"" ColumnName=""{referencePropertyInfo.Name}ID"" />";
        }

        private static string GetNavigationPropertyNodeForConceptualModel(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"    <NavigationProperty Name=""{referencePropertyInfo.Name}"" Relationship=""Self.{referencePropertyInfo.DataStructure.Module.Name}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}"" FromRole=""{referencePropertyInfo.DataStructure.Module.Name}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}_Source"" ToRole=""{referencePropertyInfo.DataStructure.Module.Name}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}_Target"" />";
        }

        private static string GetAssociationSetNameForRefrence(ReferencePropertyInfo referencePropertyInfo)
        {
            return $@"{referencePropertyInfo.DataStructure.Module}_{referencePropertyInfo.DataStructure.Name}_{referencePropertyInfo.Name}";
        }

        private static string GetEFName(DataStructureInfo dataStructureInfo)
        {
            return $@"{dataStructureInfo.Module}_{dataStructureInfo.Name}";
        }
    }
}
