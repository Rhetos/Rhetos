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
    /// <summary>
    /// Generator for FOREIGN KEY is separated from COLUMN generator (ReferencePropertyDatabaseDefinition)
    /// so that changes in foreign key options (such as on delete cascade) can be done without regenerating the column.
    /// </summary>
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyDbConstraintInfo))]
    public class ReferencePropertyConstraintDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<ReferencePropertyDbConstraintInfo> ForeignKeyConstraintOptions = "FK options";

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public ReferencePropertyConstraintDatabaseDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            this.Sql = sqlResources;
            this.SqlUtility = sqlUtility;
        }

        public string GetConstraintName(ReferencePropertyInfo reference)
        {
            return SqlUtility.Identifier(Sql.Format("ReferencePropertyConstraintDatabaseDefinition_ConstraintName",
                reference.DataStructure.Name,
                reference.Referenced.Name,
                reference.Name));
        }
        
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyDbConstraintInfo)conceptInfo;
            var reference = info.Reference;

            return Sql.Format("ReferencePropertyConstraintDatabaseDefinition_Create",
                SqlUtility.Identifier(reference.DataStructure.Module.Name) + "." + SqlUtility.Identifier(reference.DataStructure.Name),
                GetConstraintName(reference),
                SqlUtility.Identifier(reference.Name + "ID"),
                ForeignKeyUtility.GetSchemaTableForForeignKey(reference.Referenced, SqlUtility),
                ForeignKeyConstraintOptions.Evaluate(info));
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyDbConstraintInfo)conceptInfo;
            var reference = info.Reference;

            return Sql.Format("ReferencePropertyConstraintDatabaseDefinition_Remove",
                SqlUtility.Identifier(reference.DataStructure.Module.Name) + "." + SqlUtility.Identifier(reference.DataStructure.Name),
                GetConstraintName(reference));
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (ReferencePropertyDbConstraintInfo)conceptInfo;
            var reference = info.Reference;

            createdDependencies = ForeignKeyUtility.GetAdditionalForeignKeyDependencies(reference.Referenced)
                .Select(dep => Tuple.Create<IConceptInfo, IConceptInfo>(dep, reference));
        }
    }
}
