﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Rhetos.Compiler;
using Rhetos.DatabaseGenerator;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl.DefaultConcepts.DatabaseWorkarounds;
using Rhetos.Extensibility;
using Rhetos.Utilities;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(IncludeInfo))]
    public class IncludeDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        private readonly ConceptMetadata _conceptMetadata;
        private readonly IDslModel _dslModel;

        public IncludeDatabaseDefinition(ConceptMetadata conceptMetadata, IDslModel dslModel)
        {
            _conceptMetadata = conceptMetadata;
            _dslModel = dslModel;
        }

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
            var info = (IncludeInfo)conceptInfo;
            createdDependencies = null;

            var names = info.Columns.Split(' ');
            for (int i = 0; i < names.Length; i++)
            {
                var property = (PropertyInfo)_dslModel.FindByKey($"PropertyInfo {info.SqlIndex.DataStructure.FullName}.{names[i]}");
                if (property != null) names[i] = _conceptMetadata.GetColumnName(property) ?? names[i];
            }

            if (info.SqlIndex.SqlImplementation())
                codeBuilder.InsertCode(string.Join(", ", names), SqlIndexMultipleDatabaseDefinition.IncludeTag, info.SqlIndex);
        }
    }
}
