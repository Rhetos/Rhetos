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

using Rhetos.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// An internal concept for generating FK constraint in database.
    /// This concept is separate from the ReferencePropertyInfo concept to allow changes in FK constraint
    /// without making unnecessary database modifications (refresh) in features that depend on the ReferencePropertyInfo.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class ReferencePropertyDbConstraintInfo: IConceptInfo
    {
        [ConceptKey]
        public ReferencePropertyInfo Reference { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class ReferencePropertyDbConstraintMacro : IConceptMacro<InitializationConcept>
    {
        private readonly ISqlUtility _sqlUtility;

        public ReferencePropertyDbConstraintMacro(ISqlUtility sqlUtility)
        {
            _sqlUtility = sqlUtility;
        }

        public bool IsSupported(ReferencePropertyInfo reference)
        {
            return reference.DataStructure is EntityInfo
                && ForeignKeyUtility.GetSchemaTableForForeignKey(reference.Referenced, _sqlUtility) != null;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            return existingConcepts.FindByType<ReferencePropertyInfo>()
                .Where(IsSupported)
                .Select(rp => new ReferencePropertyDbConstraintInfo { Reference = rp });
        }
    }
}
