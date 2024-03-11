using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlIndexWhereInfo))]
    public class SqlWhereDatabaseDefinition: IConceptDatabaseDefinitionExtension
#pragma warning restore CS0618 // Type or member is obsolete
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
            var info = (SqlIndexWhereInfo)conceptInfo;
            createdDependencies = null;

            if (info.SqlIndex.SqlImplementation())
                codeBuilder.InsertCode("WHERE " + info.SqlFilter + " ", SqlIndexMultipleDatabaseDefinition.Options2Tag, info.SqlIndex);
        }
    }
}
