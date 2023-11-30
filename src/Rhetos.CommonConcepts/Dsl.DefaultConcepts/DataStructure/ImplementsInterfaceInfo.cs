﻿/*
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
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Data structure's POCO class implements the given C# interface.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Implements")]
    public class ImplementsInterfaceInfo : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string InterfaceType { get; set; }

        public Type GetInterfaceType()
        {
            Type type = Type.GetType(InterfaceType);
            if (type == null)
                throw new DslSyntaxException(this, "Could not find type \"" + InterfaceType + "\"");
            return type;
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            GetInterfaceType();
        }
    }

    [Export(typeof(IConceptMacro))]
    public class ImplementsInterfaceMacro : IConceptMacro<ImplementsInterfaceInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ImplementsInterfaceInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var interfaceProperties = conceptInfo.GetInterfaceType().GetProperties();
            var interfacePropertiesIndex = interfaceProperties.ToDictionary(ip => ip.Name);

            foreach (var property in existingConcepts.FindByReference<PropertyInfo>(p => p.DataStructure, conceptInfo.DataStructure))
            {
                System.Reflection.PropertyInfo interfaceProperty;
                if (interfacePropertiesIndex.TryGetValue(property.Name, out interfaceProperty))
                {
                    if (interfaceProperty.PropertyType.IsInterface)
                        newConcepts.Add(new ImplementsInterfacePropertyInfo
                            {
                                ImplementsInterface = conceptInfo,
                                Property = property,
                                PropertyInterfaceTypeName = interfaceProperty.PropertyType.FullName
                            });
                }
            }

            return newConcepts;
        }
    }
}
