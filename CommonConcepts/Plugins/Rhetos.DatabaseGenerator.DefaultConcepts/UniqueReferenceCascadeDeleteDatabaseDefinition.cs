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

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(UniqueReferenceCascadeDeleteInfo))]
    public class UniqueReferenceCascadeDeleteDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        private readonly bool _legacyCascadeDeleteInDatabase;

        public UniqueReferenceCascadeDeleteDatabaseDefinition(DeployOptions deployOptions)
        {
            _legacyCascadeDeleteInDatabase = deployOptions.CommonConcepts__Legacy__CascadeDeleteInDatabase;
        }

        public void ExtendDatabaseStructure(
            IConceptInfo conceptInfo, ICodeBuilder codeBuilder,
            out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            // Cascade delete FK in database is not needed because the server application will explicitly delete the referencing data (to ensure server-side validations and recomputations).
            // Cascade delete in database is just a legacy feature, a convenience for development and testing.
            // It is turned off by default because if a record is deleted by cascade delete directly in the database, then the business logic implemented in application layer will not be executed.
            var info = (UniqueReferenceCascadeDeleteInfo) conceptInfo;

            if (_legacyCascadeDeleteInDatabase && UniqueReferenceDatabaseDefinition.IsSupported(info.UniqueReference))
                codeBuilder.InsertCode("ON DELETE CASCADE ", UniqueReferenceDatabaseDefinition.ForeignKeyConstraintOptionsTag, info.UniqueReference);

            createdDependencies = null;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }
    }
}
