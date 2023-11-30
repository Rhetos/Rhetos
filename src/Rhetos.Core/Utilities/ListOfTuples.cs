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
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class ListOfTuples<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add(T1 e1, T2 e2)
        {
            Add(Tuple.Create(e1, e2));
        }
    }

    public class ListOfTuples<T1, T2, T3> : List<Tuple<T1, T2, T3>>
    {
        public void Add(T1 e1, T2 e2, T3 e3)
        {
            Add(Tuple.Create(e1, e2, e3));
        }
    }

    public class ListOfTuples<T1, T2, T3, T4> : List<Tuple<T1, T2, T3, T4>>
    {
        public void Add(T1 e1, T2 e2, T3 e3, T4 e4)
        {
            Add(Tuple.Create(e1, e2, e3, e4));
        }
    }
}
