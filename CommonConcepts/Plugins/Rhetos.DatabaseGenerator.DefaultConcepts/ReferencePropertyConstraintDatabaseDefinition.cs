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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    /// <summary>
    /// Generator for FOREIGN KEY is separated from COLUMN generator (ReferencePropertyDatabaseDefinition)
    /// so that changes in foreign key options (such as on delete cascade) can be done without regenerating the column.
    /// </summary>
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(ReferencePropertyDatabaseDefinition))]
    public class ReferencePropertyConstraintDatabaseDefinition : IConceptDatabaseDefinition, IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<ReferencePropertyInfo> ForeignKeyConstraintOptions = "FK options";

        public static string GetConstraintName(ReferencePropertyInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("ReferencePropertyConstraintDatabaseDefinition_ConstraintName",
                info.DataStructure.Name,
                info.Referenced.Name,
                info.Name));
        }

        public static bool IsSupported(ReferencePropertyInfo info)
        {
            return ReferencePropertyDatabaseDefinition.IsSupported(info)
                && ForeignKeyUtility.GetSchemaTableForForeignKey(info.Referenced) != null;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;

            if (IsSupported(info))
                return Sql.Format("ReferencePropertyConstraintDatabaseDefinition_Create",
                    SqlUtility.Identifier(info.DataStructure.Module.Name) + "." + SqlUtility.Identifier(info.DataStructure.Name),
                    GetConstraintName(info),
                    info.GetColumnName(),
                    ForeignKeyUtility.GetSchemaTableForForeignKey(info.Referenced),
                    ForeignKeyConstraintOptions.Evaluate(info));
            return "";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;

            if (IsSupported(info))
                return Sql.Format("ReferencePropertyConstraintDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.DataStructure.Module.Name) + "." + SqlUtility.Identifier(info.DataStructure.Name),
                    GetConstraintName(info));
            return "";
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (ReferencePropertyInfo)conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();

            if (IsSupported(info))
                dependencies.AddRange(ForeignKeyUtility.GetAdditionalForeignKeyDependencies(info.Referenced)
                    .Select(dep => Tuple.Create<IConceptInfo, IConceptInfo>(dep, info)));

            createdDependencies = dependencies;
        }
    }
}
