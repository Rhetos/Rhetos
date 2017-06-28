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
using System.Text;
using System.Text.RegularExpressions;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Each detail data structure in the module will inherit row permissions from it's mater data structure.
    /// Each extension in the module will inherit row permissions from it's base data structure.
    /// Row permissions can be inherited from other modules to this module.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutoInheritRowPermissions")]
    public class AutoInheritRowPermissionsInfo : IConceptInfo
    {
        [ConceptKey]
        public ModuleInfo Module { get; set; }
    }

    /// <summary>
    /// Each detail data structure in the module will inherit row permissions from it's mater data structure.
    /// Each extension in the module will inherit row permissions from it's base data structure.
    /// Row permissions will not be inherited from other modules to this module.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutoInheritRowPermissionsInternally")]
    public class AutoInheritRowPermissionsInternallyInfo : AutoInheritRowPermissionsInfo
    {
    }

    [Export(typeof(IConceptMacro))]
    public class AutoInheritRowPermissionsMacro : IConceptMacro<InitializationConcept>
    {
        /// <summary>
        /// This macro implements InitializationConcept (singleton) instead of AutoInheritRowPermissionsInfo,
        /// in order to allow creating the new concepts in a single iteration.
        /// </summary>
        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            var autoInheritModules = new HashSet<string>(
                existingConcepts.FindByType<AutoInheritRowPermissionsInfo>().Select(airp => airp.Module.Name));
            var internalInheritanceModules = new HashSet<string>(
                existingConcepts.FindByType<AutoInheritRowPermissionsInternallyInfo>().Select(airp => airp.Module.Name));

            var autoInheritExtensionsByBase = new MultiDictionary<string, DataStructureExtendsInfo>();
            var autoInheritExtensions = existingConcepts.FindByType<DataStructureExtendsInfo>()
                .Where(e => autoInheritModules.Contains(e.Extension.Module.Name))
                .Where(e => e.Extension.Module == e.Base.Module || !internalInheritanceModules.Contains(e.Extension.Module.Name));
            foreach (var autoInheritExtension in autoInheritExtensions)
                autoInheritExtensionsByBase.Add(autoInheritExtension.Base.GetKey(), autoInheritExtension);

            var autoInheritDetailsByMaster = new MultiDictionary<string, ReferenceDetailInfo>();
            var autoInheritDetails = existingConcepts.FindByType<ReferenceDetailInfo>()
                .Where(d => autoInheritModules.Contains(d.Reference.DataStructure.Module.Name))
                .Where(d => d.Reference.Referenced.Module == d.Reference.DataStructure.Module || !internalInheritanceModules.Contains(d.Reference.DataStructure.Module.Name));
            foreach (var autoInheritDetail in autoInheritDetails)
                autoInheritDetailsByMaster.Add(autoInheritDetail.Reference.Referenced.GetKey(), autoInheritDetail);

            var rowPermissionsRead = existingConcepts.FindByType<RowPermissionsReadInfo>();
            var rowPermissionsWrite = existingConcepts.FindByType<RowPermissionsWriteInfo>();
            var allDataStructuresWithRowPermissions = new HashSet<string>(
                rowPermissionsRead.Select(rp => rp.Source.GetKey())
                .Union(rowPermissionsWrite.Select(rp => rp.Source.GetKey())).ToList());

            var newConcepts = new List<IConceptInfo>();

            var newDataStructuresWithRowPermissions = new List<string>(allDataStructuresWithRowPermissions);
            while (newDataStructuresWithRowPermissions.Count > 0)
            {
                var newInheritences = new List<IConceptInfo>();

                newInheritences.AddRange(newDataStructuresWithRowPermissions
                    .SelectMany(ds => autoInheritExtensionsByBase.Get(ds))
                    .SelectMany(extension =>
                    {
                        var rpFilters = new RowPermissionsPluginableFiltersInfo { DataStructure = extension.Extension };
                        var rpInherit = new RowPermissionsInheritFromBaseInfo { RowPermissionsFilters = rpFilters };
                        return new IConceptInfo[] { rpFilters, rpInherit };
                    }));

                newInheritences.AddRange(newDataStructuresWithRowPermissions
                    .SelectMany(ds => autoInheritDetailsByMaster.Get(ds))
                    .SelectMany(detail =>
                    {
                        var rpFilters = new RowPermissionsPluginableFiltersInfo { DataStructure = detail.Reference.DataStructure };
                        var rpInherit = new RowPermissionsInheritFromReferenceInfo { RowPermissionsFilters = rpFilters, ReferenceProperty = detail.Reference };
                        return new IConceptInfo[] { rpFilters, rpInherit };
                    }));

                newConcepts.AddRange(newInheritences);

                newDataStructuresWithRowPermissions = newInheritences.OfType<RowPermissionsPluginableFiltersInfo>()
                    .Select(rpFilters => rpFilters.DataStructure.GetKey())
                    .Where(dataStructure => !allDataStructuresWithRowPermissions.Contains(dataStructure))
                    .ToList();

                foreach (var dataStructure in newDataStructuresWithRowPermissions)
                    allDataStructuresWithRowPermissions.Add(dataStructure);
            };

            return newConcepts;
        }
    }
}
