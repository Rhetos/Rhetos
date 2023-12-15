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
    [ExportMetadata(MefProvider.Implements, typeof(UniqueWhereInfo))]
    public class UniqueWhereDatabaseDefinition: IConceptDatabaseDefinitionExtension
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
            var info = (UniqueWhereInfo)conceptInfo;
            createdDependencies = null;

            if (info.Dependency_SqlIndex.SqlImplementation())
                codeBuilder.InsertCode("WHERE " + info.SqlFilter + " ", SqlIndexMultipleDatabaseDefinition.Options2Tag, info.Dependency_SqlIndex);
        }
    }
}
