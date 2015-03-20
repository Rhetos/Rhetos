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
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    public class ExtensibleSubtypeSqlViewInfo : SqlViewInfo, IAlternativeInitializationConcept
    {
        public IsSubtypeOfInfo IsSubtypeOf { get; set; }

        public static readonly SqlTag<ExtensibleSubtypeSqlViewInfo> PropertyImplementationTag = "PropertyImplementation";

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Module", "Name", "ViewSource" };
        }

        void IAlternativeInitializationConcept.InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            var prototype = IsSubtypeOf.GetImplementationViewPrototype();

            Module = prototype.Module;
            Name = prototype.Name;
            ViewSource = string.Format("SELECT\r\n    ID{3}{2}\r\nFROM\r\n    {0}.{1}",
                IsSubtypeOf.Subtype.Module.Name,
                IsSubtypeOf.Subtype.Name,
                PropertyImplementationTag.Evaluate(this),
                GetSubtypeImplementationIdSnippet());

            createdConcepts = null;
        }

        /// <summary>
        /// Same subtype may implement same supertype multiple time. Since ID of the supertype is usually same as subtype's ID,
        /// that might result with multiple supertype records with the same ID. To avoid duplicate IDs and still keep the
        /// deterministic ID values, the supertype's ID is XORed by a hash code taken from the ImplementationName.
        /// </summary>
        private string GetSubtypeImplementationIdSnippet()
        {
            if (IsSubtypeOf.ImplementationName == "")
                return "";
            else if (IsSubtypeOf.SupportsPersistedSubtypeImplementationColum())
            {
                var persistedColumnPrototype = new PersistedSubtypeImplementationIdInfo { Subtype = IsSubtypeOf.Subtype, ImplementationName = IsSubtypeOf.ImplementationName };
                return ",\r\n    SubtypeImplementationID = " + persistedColumnPrototype.GetComputedColumnName();
            }
            else
            {
                int hash = DomUtility.GetSubtypeImplementationHash(IsSubtypeOf.ImplementationName);
                return ",\r\n    SubtypeImplementationID = CONVERT(UNIQUEIDENTIFIER, CONVERT(BINARY(4), CONVERT(INT, CONVERT(BINARY(4), ID)) ^ " + hash + ") + SUBSTRING(CONVERT(BINARY(16), ID), 5, 12))";
            }
        }
    }

    [Export(typeof(IConceptMacro))]
    public class ExtensibleSubtypeSqlViewMacro : IConceptMacro<ExtensibleSubtypeSqlViewInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ExtensibleSubtypeSqlViewInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Automatic interface implementation: Add missing property implementations and missing properties to the subtype.

            var implementableSupertypeProperties = existingConcepts.FindByType<PolymorphicPropertyInfo>()
                .Where(pp => pp.Property.DataStructure == conceptInfo.IsSubtypeOf.Supertype && pp.IsImplementable())
                .Select(pp => pp.Property).ToList();
            var subtypeProperties = existingConcepts.FindByReference<PropertyInfo>(p => p.DataStructure, conceptInfo.IsSubtypeOf.Subtype);
            var subtypeImplementsProperties = existingConcepts.FindByReference<SubtypeImplementsPropertyInfo>(subim => subim.IsSubtypeOf, conceptInfo.IsSubtypeOf)
                .Select(subim => subim.Property).ToList();

            var missingImplementations = implementableSupertypeProperties.Except(subtypeImplementsProperties)
                .Select(missing => new SubtypeImplementsPropertyInfo
                {
                    IsSubtypeOf = conceptInfo.IsSubtypeOf,
                    Property = missing,
                    Expression = GetColumnName(missing)
                })
                .ToList();

            var missingProperties = missingImplementations.Select(subim => subim.Property).Where(supp => !subtypeProperties.Any(subp => subp.Name == supp.Name))
                .Select(missing => DslUtility.CreatePassiveClone(missing, conceptInfo.IsSubtypeOf.Subtype))
                .ToList();

            newConcepts.AddRange(missingImplementations);
            newConcepts.AddRange(missingProperties);

            return newConcepts;
        }

        private static string GetColumnName(PropertyInfo property)
        {
            if (property is ReferencePropertyInfo)
                return SqlUtility.Identifier(property.Name + "ID");
            return SqlUtility.Identifier(property.Name);
        }
    }
}
