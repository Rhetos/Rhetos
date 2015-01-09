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
using System.Diagnostics.Contracts;
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    // No need for ExportAttribute, this class is registered manually.
    public class DslModelIndexByType : IDslModelIndex
    {
        public void Add(IConceptInfo concept)
        {
            _conceptsByType.Add(concept.GetType(), concept);
        }

        public IEnumerable<IConceptInfo> FindByType(Type conceptType, bool includeDerivations)
        {
            if (includeDerivations)
                return _conceptsByType
                    .Where(conceptsGroup => conceptType.IsAssignableFrom(conceptsGroup.Key))
                    .SelectMany(conceptsGroup => conceptsGroup.Value);
            else
            {
                List<IConceptInfo> result = null;
                if (_conceptsByType.TryGetValue(conceptType, out result))
                    return result;
                return new List<IConceptInfo>();
            }
        }

        private readonly MultiDictionary<Type, IConceptInfo> _conceptsByType = new MultiDictionary<Type, IConceptInfo>();
    }

    public static class DslModelIndexerByTypeExtensions
    {
        public static IEnumerable<T> FindByType<T>(this IDslModel dslModel) where T : IConceptInfo
        {
            return dslModel.GetIndex<DslModelIndexByType>().FindByType(typeof(T), includeDerivations: true).Cast<T>();
        }

        public static IEnumerable<IConceptInfo> FindByType(this IDslModel dslModel, Type conceptType)
        {
            return dslModel.GetIndex<DslModelIndexByType>().FindByType(conceptType, includeDerivations: true);
        }

        public static IEnumerable<IConceptInfo> FindByType(this IDslModel dslModel, Type conceptType, bool includeDerivations)
        {
            return dslModel.GetIndex<DslModelIndexByType>().FindByType(conceptType, includeDerivations);
        }
    }
}
