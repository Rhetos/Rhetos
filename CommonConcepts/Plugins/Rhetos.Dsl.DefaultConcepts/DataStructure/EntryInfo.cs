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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Entry")]
    public class EntryInfo : IMacroConcept
    {
        [ConceptKey]
        public HardcodedEntityInfo HardcodedEntity { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        public Guid GetIdentifier()
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(Name);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

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
    }
}
