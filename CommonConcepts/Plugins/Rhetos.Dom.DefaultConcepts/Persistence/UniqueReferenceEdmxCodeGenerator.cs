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
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureEdmxCodeGenerator))]
    public class UniqueReferenceEdmxCodeGenerator : IEdmxCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var uniqueReferenceInfo = conceptInfo as UniqueReferenceInfo;

            if (uniqueReferenceInfo.Base is IOrmDataStructure && uniqueReferenceInfo.Extension is IOrmDataStructure)
            {
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModelForExtension(uniqueReferenceInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(uniqueReferenceInfo.Base));
                codeBuilder.InsertCode(GetNavigationPropertyNodeForConceptualModel(uniqueReferenceInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypeNavigationPropertyTag.Evaluate(uniqueReferenceInfo.Extension));
                codeBuilder.InsertCode(GetAssociationNodeForConceptualModel(uniqueReferenceInfo), EdmxInitialCodeSnippet.ConceptualModelAssociationTag);

                codeBuilder.InsertCode(GetAssociationSetNodeForConceptualModel(uniqueReferenceInfo), EdmxInitialCodeSnippet.ConceptualModelAssociationSetTag);

                codeBuilder.InsertCode(GetAssociationNodeForStorageModel(uniqueReferenceInfo), EdmxInitialCodeSnippet.StorageModelAssociationTag);
                codeBuilder.InsertCode(GetAttributeForIDPropertyForStorageModel(), DataStructureEdmxCodeGenerator.StorageModelCustomannotationIndexForIDTag.Evaluate(uniqueReferenceInfo.Extension));
                codeBuilder.InsertCode(GetAssociationSetNodeForStorageModel(uniqueReferenceInfo), EdmxInitialCodeSnippet.StorageModelAssociationSetTag);
            }
        }

        private static string GetAssociationSetNodeForStorageModel(UniqueReferenceInfo extension)
        {
            return "\n" + $@"  <AssociationSet Name=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}"" Association=""Self.{GetAssociationSetNameForConceptualModelForExtensions(extension)}"">
    <End Role=""{GetEFName(extension.Base)}"" EntitySet=""{GetEFName(extension.Base)}"" />
    <End Role=""{GetEFName(extension.Extension)}"" EntitySet=""{GetEFName(extension.Extension)}"" />
  </AssociationSet>";
        }

        private static string GetAssociationSetNodeForConceptualModel(UniqueReferenceInfo extension)
        {
            return "\n" + $@"	<AssociationSet Name=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}"" Association=""Self.{GetAssociationSetNameForConceptualModelForExtensions(extension)}"">
		<End Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Source"" EntitySet=""{GetEFName(extension.Extension)}"" />
		<End Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Target"" EntitySet=""{GetEFName(extension.Base)}"" />
	</AssociationSet>";
        }

        private static string GetAssociationNodeForStorageModel(UniqueReferenceInfo extension)
        {
            return "\n" + $@"  <Association Name=""{extension.Extension.Module}_{extension.Extension.Name}_Base"">
    <End Role=""{extension.Base.Module}_{extension.Base.Name}"" Type=""Self.{extension.Base.Module}_{extension.Base.Name}"" Multiplicity=""1"" />
    <End Role=""{extension.Extension.Module}_{extension.Extension.Name}"" Type=""Self.{extension.Extension.Module}_{extension.Extension.Name}"" Multiplicity=""0..1"" />
    <ReferentialConstraint>
      <Principal Role=""{extension.Base.Module}_{extension.Base.Name}"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{extension.Extension.Module}_{extension.Extension.Name}"">
        <PropertyRef Name=""ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAssociationNodeForConceptualModel(UniqueReferenceInfo extension)
        {
            return "\n" + $@"  <Association Name=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}"">
    <End Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Source"" Type=""Self.{extension.Extension.Module}_{extension.Extension.Name}"" Multiplicity=""0..1"" />
    <End Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Target"" Type=""Self.{extension.Base.Module}_{extension.Base.Name}"" Multiplicity=""1"" />
    <ReferentialConstraint>
      <Principal Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Target"">
        <PropertyRef Name=""ID"" />
      </Principal>
      <Dependent Role=""{GetAssociationSetNameForConceptualModelForExtensions(extension)}_Source"">
        <PropertyRef Name=""ID"" />
      </Dependent>
    </ReferentialConstraint>
  </Association>";
        }

        private static string GetAttributeForIDPropertyForStorageModel()
        {
            return "\n" + $@" customannotation:Index=""{{ Name: IX_ID, Order: 0 }}""";
        }

        private static string GetNavigationPropertyNodeForConceptualModel(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return "\n" + $@"    <NavigationProperty Name=""Base"" Relationship=""Self.{GetEFName(uniqueReferenceInfo.Extension)}_Base"" FromRole=""{GetEFName(uniqueReferenceInfo.Extension)}_Base_Source"" ToRole=""{GetEFName(uniqueReferenceInfo.Extension)}_Base_Target"" />";
        }

        private static string GetNavigationPropertyNodeForConceptualModelForExtension(UniqueReferenceInfo uniqueReferenceInfo)
        {
            return "\n" + $@"    <NavigationProperty Name=""{GetNavigationPropertyNameForConceptualModelForExtension(uniqueReferenceInfo)}"" Relationship=""Self.{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}_Base"" FromRole=""{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}_Base_Target"" ToRole=""{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}_Base_Source"" />";
        }

        private static string GetNavigationPropertyNameForConceptualModelForExtension(UniqueReferenceInfo uniqueReferenceInfo)
        {
            if (uniqueReferenceInfo.Base.Module.Name.Equals(uniqueReferenceInfo.Extension.Module.Name))
                return $@"Extension_{uniqueReferenceInfo.Extension.Name}";
            else
                return $@"Extension_{uniqueReferenceInfo.Extension.Module.Name}_{uniqueReferenceInfo.Extension.Name}";
        }

        private static string GetAssociationSetNameForConceptualModelForExtensions(UniqueReferenceInfo extensionInfo)
        {
            return $@"{extensionInfo.Extension.Module}_{extensionInfo.Extension.Name}_Base";
        }

        private static string GetEFName(DataStructureInfo dataStructureInfo)
        {
            return $@"{dataStructureInfo.Module}_{dataStructureInfo.Name}";
        }
    }
}
