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
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceInfo))]
    public class UniqueReferenceDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<UniqueReferenceInfo> ForeignKeyConstraintOptionsTag = "FK options";

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public UniqueReferenceDatabaseDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            this.Sql = sqlResources;
            this.SqlUtility = sqlUtility;
        }

        public string GetConstraintName(UniqueReferenceInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("UniqueReferenceDatabaseDefinition_ConstraintName",
                info.Extension.Name,
                info.Base.Name));
        }

        public bool IsSupported(UniqueReferenceInfo info)
        {
            return info.Extension is EntityInfo
                && ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base, SqlUtility) != null;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (UniqueReferenceInfo)conceptInfo;
            if (IsSupported(info))
            {
                return Sql.Format("UniqueReferenceDatabaseDefinition_Create",
                    SqlUtility.Identifier(info.Extension.Module.Name) + "." + SqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info),
                    ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base, SqlUtility),
                    ForeignKeyConstraintOptionsTag.Evaluate(info));
            }
            // TODO: else - Generate a Filter+InvalidData validation in the server application that checks for invalid items.
            return "";
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (UniqueReferenceInfo) conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();
            dependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(info.Base, info.Extension));

            if (IsSupported(info))
                dependencies.AddRange(ForeignKeyUtility.GetAdditionalForeignKeyDependencies(info.Base)
                    .Select(dep => Tuple.Create<IConceptInfo, IConceptInfo>(dep, info))
                    .ToList());

            createdDependencies = dependencies;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (UniqueReferenceInfo)conceptInfo;
            if (IsSupported(info))
            {
                return Sql.Format("UniqueReferenceDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.Extension.Module.Name) + "." + SqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info));
            }
            return "";
        }
    }
}
