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
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptMacro))]
    public class RowPermissionsInheritExtensionMacro : IConceptMacro<InitializationConcept>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var extensionsIndex = existingConcepts.FindByType<UniqueReferenceInfo>()
                .ToDictionary(e => e.Extension.GetKeyProperties(), e => e);

            newConcepts.AddRange(existingConcepts.FindByType<RowPermissionsInheritReadInfo>()
                .Where(ir => ir.InheritFromInfo.SourceSelector == "Base")
                .Select(ir => new RowPermissionsInheritExtensionReadInfo
                {
                    InheritRead = ir,
                    Extends = extensionsIndex.GetValueOrDefault(ir.InheritFromInfo.RowPermissionsFilters.DataStructure.GetKeyProperties())
                })
                .Where(ier => ier.Extends != null));

            newConcepts.AddRange(existingConcepts.FindByType<RowPermissionsInheritWriteInfo>()
                .Where(iw => iw.InheritFromInfo.SourceSelector == "Base")
                .Select(iw => new RowPermissionsInheritExtensionWriteInfo
                {
                    InheritWrite = iw,
                    Extends = extensionsIndex.GetValueOrDefault(iw.InheritFromInfo.RowPermissionsFilters.DataStructure.GetKeyProperties())
                })
                .Where(iew => iew.Extends != null));

            return newConcepts;
        }
    }
}
