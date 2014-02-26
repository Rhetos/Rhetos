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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Security;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(SaveEntityCommandInfo))]
    public class SaveCommandClaims : IClaimProvider
    {
        public IList<Claim> GetRequiredClaims(ICommandInfo info)
        {
            SaveEntityCommandInfo commandInfo = (SaveEntityCommandInfo) info;
            List<Claim> claims = new List<Claim>();

            if (commandInfo.DataToInsert != null && commandInfo.DataToInsert.Length > 0)
                claims.Add(new Claim(commandInfo.Entity, "New"));

            if (commandInfo.DataToUpdate != null && commandInfo.DataToUpdate.Length > 0)
                claims.Add(new Claim(commandInfo.Entity, "Edit"));

            if (commandInfo.DataToDelete != null && commandInfo.DataToDelete.Length > 0)
                claims.Add(new Claim(commandInfo.Entity, "Remove"));

            return claims;
        }

        public IList<Claim> GetAllClaims(IDslModel dslModel)
        {
            var writableDataStructures = dslModel.Concepts.OfType<DataStructureInfo>()
                    .Where(dataStructure => dataStructure is IWritableOrmDataStructure)
                    .Union(
                        dslModel.Concepts.OfType<WriteInfo>()
                        .Select(x => x.DataStructure)).ToArray();

            return writableDataStructures.SelectMany(dataStructure => new Claim[]
                {
                    new Claim(dataStructure.GetKeyProperties(), "New"),
                    new Claim(dataStructure.GetKeyProperties(), "Edit"),
                    new Claim(dataStructure.GetKeyProperties(), "Remove")
                }).ToList();
        }
    }
}