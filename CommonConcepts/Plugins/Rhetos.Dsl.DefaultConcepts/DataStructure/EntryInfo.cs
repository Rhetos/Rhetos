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
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Creates a row in the database for every entry inside a Hardcoded concept.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Entry")]
    public class EntryInfo : IMacroConcept, IValidatedConcept
    {
        [ConceptKey]
        public HardcodedEntityInfo HardcodedEntity { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        public Guid GetIdentifier()
        {
            using (var hashing = System.Security.Cryptography.SHA256.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(Name);
                byte[] hashBytes = hashing.ComputeHash(inputBytes).Take(16).ToArray();
                return new Guid(hashBytes);
            }
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var sqlFunction = new SqlFunctionInfo
            {
                Module = HardcodedEntity.Module,
                Name = HardcodedEntity.Name + "_" + Name,
                Source = $@"
RETURNS uniqueidentifier
AS
BEGIN
	RETURN CONVERT(uniqueidentifier, '{GetIdentifier()}');
END"
            };
            return new List<IConceptInfo>
            {
                sqlFunction,
                new SqlDependsOnSqlFunctionInfo { Dependent = this.HardcodedEntity, DependsOn = sqlFunction },
            };
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.ValidateIdentifier(Name, this);
        }
    }
}
