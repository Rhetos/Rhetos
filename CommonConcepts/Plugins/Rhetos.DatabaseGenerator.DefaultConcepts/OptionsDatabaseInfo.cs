using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.DatabaseGenerator;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(OptionsInfo))]
    public class OptionsDatabaseInfo : IConceptDatabaseDefinitionExtension
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (OptionsInfo)conceptInfo;
            createdDependencies = null;

            if (info.SqlIndex.SqlImplementation())
                codeBuilder.InsertCode(info.SqlOptions + ' ', SqlIndexMultipleDatabaseDefinition.Options2Tag, info.SqlIndex);
        }
    }
}
