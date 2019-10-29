using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DomInitializationCodeGenerator))]
    public class ContainsIdsInterceptorCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode("AddInterceptor(new Rhetos.Dom.DefaultConcepts.ContainsIdsInterceptor());\r\n            ", DomInitializationCodeGenerator.EntityFrameworkConfigurationTag);
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.ContainsIdsInterceptor));
        }
    }
}
