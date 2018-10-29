using Rhetos.Compiler;
using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Persistence
{
    public class EdmxInitialCodeSnippet : IEdmxCodeGenerator
    {
        public const string ConceptualModeEntityTypeTag = "<!--ConceptualModeEntityType-->";
        public const string ConceptualModeEntitySetTag = "<!--ConceptualModeEntitySet-->";
        public const string ConceptualModelAssociationSetTag = "<!--ConceptualModelAssociationSet-->";
        public const string ConceptualModelAssociationTag = "<!--ConceptualModelAssociation-->";
        public const string MappingTag = "<!--Mapping-->";
        public const string StorageModeEntityTypeTag = "<!--StorageModeEntityType-->";
        public const string StorageModeEntitySetTag = "<!--StorageModeEntitySet-->";
        public const string StorageModelAssociationSetTag = "<!--StorageModelAssociationSet-->";
        public const string StorageModelAssociationTag = "<!--StorageModelAssociation-->";

        public const string SegmentSplitter = "<!--SegmentSplitter-->";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(
$@"{ConceptualModeEntityTypeTag}{ConceptualModelAssociationTag}
  <EntityContainer Name=""EntityFrameworkContext"" customannotation:UseClrTypes=""true"">{ConceptualModeEntitySetTag}{ConceptualModelAssociationSetTag}
  </EntityContainer>
{SegmentSplitter}
  <EntityContainerMapping StorageEntityContainer=""CodeFirstDatabase"" CdmEntityContainer=""EntityFrameworkContext"">{MappingTag}
  </EntityContainerMapping>
{ SegmentSplitter}
{StorageModeEntityTypeTag}{StorageModelAssociationTag}
  <EntityContainer Name=""CodeFirstDatabase"">{StorageModeEntitySetTag}{StorageModelAssociationSetTag}
  </EntityContainer>"
            );
        }
    }
}
