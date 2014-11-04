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
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferenceCascadeDeleteInfo))]
    public class ReferenceCascadeDeleteDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public void ExtendDatabaseStructure(
            IConceptInfo conceptInfo, ICodeBuilder codeBuilder, 
            out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            // Cascade delete FK in database is not essential because the server application will explicitly delete the referencing data (to ensure server-side validations and recomputations).
			// Cascade delete in database is just a convenience for development and testing.
            var info = (ReferenceCascadeDeleteInfo)conceptInfo;
            createdDependencies = null;

            if (ReferencePropertyConstraintDatabaseDefinition.IsSupported(info.Reference))
            {
                codeBuilder.InsertCode(Sql.Get("ReferenceCascadeDeleteDatabaseDefinition_ExtendForeignKey"),
                    ReferencePropertyConstraintDatabaseDefinition.ForeignKeyConstraintOptions, info.Reference);
            }
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return "";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return "";
        }
    }
}
