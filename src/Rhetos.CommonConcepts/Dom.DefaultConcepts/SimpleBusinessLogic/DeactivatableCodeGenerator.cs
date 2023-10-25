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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DeactivatableInfo))]
    public class DeactivatableCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DeactivatableInfo)conceptInfo;

            string saveCode = @"DeactivatableCodeGenerator.SetActiveByDefault(insertedNew, updatedNew, updated);

            ";

            codeBuilder.InsertCode(saveCode, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Entity);
            codeBuilder.InsertCode("Rhetos.Dom.DefaultConcepts.IDeactivatable", DataStructureCodeGenerator.InterfaceTag, info.Entity);
        }

        public static void SetActiveByDefault(IEnumerable<IDeactivatable> insertedNew, IEnumerable<IDeactivatable> updatedNew, IEnumerable<IDeactivatable> updatedOld)
        {
            foreach (var newItem in insertedNew)
                if (newItem.Active == null)
                    newItem.Active = true;

            foreach (var change in updatedNew.Zip(updatedOld, (newItem, oldItem) => new { newItem, oldItem }))
                if (change.newItem.Active == null)
                    change.newItem.Active = change.oldItem.Active ?? true;
        }
    }
}
