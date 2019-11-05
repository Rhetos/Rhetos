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
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SamePropertyValue")]
    public class SamePropertyValueInfo : IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo DerivedProperty { get; set; }

        [ConceptKey]
        public string Path { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.ValidatePath(DerivedProperty.DataStructure, Path, existingConcepts, this);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class SamePropertyValueMacro : IConceptMacro<InitializationConcept>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var samePropertiesByInheritance = existingConcepts.FindByType<SamePropertyValueInfo>()
                .Select(x => new {
                    x.DerivedProperty,
                    BaseSelector = x.Path.Substring(0, x.Path.LastIndexOf('.')),
                    BaseProperty = DslUtility.GetPropertyByPath(x.DerivedProperty.DataStructure, x.Path, existingConcepts),
                })
                .Where(x => !x.BaseProperty.IsError) // Ignore errors here, the referenced object might be created later in another macro iteration. Any remaining errors will be reported later in SamePropertyValueInfo.CheckSemantics.
                .GroupBy(same => new {
                    Module = same.DerivedProperty.DataStructure.Module.Name,
                    DataStructure = same.DerivedProperty.DataStructure.Name,
                    same.BaseSelector
                })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var inherit in existingConcepts.FindByType<RowPermissionsInheritReadInfo>())
            {
                var key = new
                {
                    Module = inherit.InheritFromInfo.RowPermissionsFilters.DataStructure.Module.Name,
                    DataStructure = inherit.InheritFromInfo.RowPermissionsFilters.DataStructure.Name,
                    BaseSelector = inherit.InheritFromInfo.SourceSelector
                };
                var optimizeProperties = samePropertiesByInheritance.GetValueOrEmpty(key);
                newConcepts.AddRange(optimizeProperties
                    .Select(op =>
                        new RowPermissionsInheritReadSameMemberInfo
                        {
                            InheritRead = inherit,
                            BaseMemberName = op.BaseProperty.Value.Name,
                            DerivedMemberName = op.DerivedProperty.Name
                        }));
                newConcepts.AddRange(optimizeProperties
                    .Where(op => op.DerivedProperty is ReferencePropertyInfo && op.BaseProperty.Value is ReferencePropertyInfo)
                    .Select(op =>
                        new RowPermissionsInheritReadSameMemberInfo
                        {
                            InheritRead = inherit,
                            BaseMemberName = op.BaseProperty.Value.Name + "ID",
                            DerivedMemberName = op.DerivedProperty.Name + "ID"
                        }));
            }

            foreach (var inherit in existingConcepts.FindByType<RowPermissionsInheritWriteInfo>())
            {
                var key = new
                {
                    Module = inherit.InheritFromInfo.RowPermissionsFilters.DataStructure.Module.Name,
                    DataStructure = inherit.InheritFromInfo.RowPermissionsFilters.DataStructure.Name,
                    BaseSelector = inherit.InheritFromInfo.SourceSelector
                };
                var optimizeProperties = samePropertiesByInheritance.GetValueOrEmpty(key);
                newConcepts.AddRange(optimizeProperties
                    .Select(op =>
                        new RowPermissionsInheritWriteSameMemberInfo
                        {
                            InheritWrite = inherit,
                            BaseMemberName = op.BaseProperty.Value.Name,
                            DerivedMemberName = op.DerivedProperty.Name
                        }));
                newConcepts.AddRange(optimizeProperties
                    .Where(op => op.DerivedProperty is ReferencePropertyInfo && op.BaseProperty.Value is ReferencePropertyInfo)
                    .Select(op =>
                        new RowPermissionsInheritWriteSameMemberInfo
                        {
                            InheritWrite = inherit,
                            BaseMemberName = op.BaseProperty.Value.Name + "ID",
                            DerivedMemberName = op.DerivedProperty.Name + "ID"
                        }));
            }

            return newConcepts;
        }
    }
}
