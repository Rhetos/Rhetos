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

using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.CommonConcepts.Test.Mocks
{
    public class DslModelMock : List<IConceptInfo>, IDslModel
    {
        public IEnumerable<IConceptInfo> Concepts { get { return this; } }

        public IConceptInfo FindByKey(string conceptKey)
        {
            return this.Where(c => c.GetKey() == conceptKey).SingleOrDefault();
        }

        public IEnumerable<IConceptInfo> FindByType(Type conceptType)
        {
            return this.Where(c => conceptType.IsAssignableFrom(c.GetType()));
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            IDslModelIndex index = (IDslModelIndex)typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });
            foreach (var concept in Concepts)
                index.Add(concept);
            return (T)index;
        }
    }
}
