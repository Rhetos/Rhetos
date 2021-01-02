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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

        /// <summary>
        /// ID property value (GUID).
        /// </summary>
        public string Identifier { get; set; }

        public Guid GetIdentifier()
        {
            //We are calling GetIdentifier in the CreateNewConcepts method but the macro evaluation will happen before the call to CheckSemantics.
            //So in order the display the message correctly we need to call ParseIdentifier.
            Guid guid;
            ParseIdentifier(out guid);
            return guid;
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            ParseIdentifier(out _);
            DslUtility.ValidateIdentifier(Name, this);
        }

        private void ParseIdentifier(out Guid guid)
        {
            if (!Guid.TryParseExact(Identifier, "D", out guid))
                throw new DslSyntaxException($"The property '{nameof(Identifier)}' for '{this.GetUserDescription()}' should be in the format '00000000-0000-0000-0000-000000000000', instead it is '{Identifier}'.");
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
    }

    /// <summary>
    /// Creates a row in the database for every entry inside a Hardcoded concept.
    /// ID value is automatically generated based on the entry name.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Entry")]
    public class EntryWithGeneratedIdentifierInfo : EntryInfo, IAlternativeInitializationConcept
    {
        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Identifier) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            createdConcepts = new List<IConceptInfo>();
            Identifier = CsUtility.GenerateGuid(Name).ToString();
        }
    }
}
