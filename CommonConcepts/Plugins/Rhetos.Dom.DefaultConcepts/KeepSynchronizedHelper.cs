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

using Rhetos.Extensibility;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// The 'OptimizeFiltersUnion' functions returns the union of the given filters, if able to optimize them to a single filter.
    /// </summary>
    public static class KeepSynchronizedHelper
    {
        public static bool OptimizeFiltersUnion<T>(T a, T b, out T union)
        {
            union = a;
            return a.Equals(b);
        }

        public static bool OptimizeFiltersUnion(IEnumerable<Guid> a, IEnumerable<Guid> b, out IEnumerable<Guid> union)
        {
            union = a.Union(b).ToList();
            return true;
        }

        public static bool OptimizeFiltersUnion(List<Guid> a, List<Guid> b, out List<Guid> union)
        {
            union = a.Union(b).ToList();
            return true;
        }

        public static bool OptimizeFiltersUnion(Guid[] a, Guid[] b, out Guid[] union)
        {
            union = a.Union(b).ToArray();
            return true;
        }

        public static bool OptimizeFiltersUnion(FilterAll a, FilterAll b, out FilterAll union)
        {
            union = a;
            return true;
        }

        public static bool OptimizeFiltersUnion(FilterSubtype a, FilterSubtype b, out FilterSubtype union)
        {
            if (a.Subtype == b.Subtype && a.ImplementationName == b.ImplementationName)
            {
                union = new FilterSubtype
                {
                    Ids = a.Ids.Union(b.Ids).ToList(),
                    Subtype = a.Subtype,
                    ImplementationName = a.ImplementationName
                };
                return true;
            }

            union = null;
            return false;
        }

        public static bool OptimizeFiltersUnion(FilterCriteria[] a, FilterCriteria[] b, out FilterCriteria[] union)
        {
            if (a.Length == 0)
            {
                union = b;
                return true;
            }
            if (b.Length == 0)
            {
                union = a;
                return true;
            }
            if (a.Length == 1 && b.Length == 1)
            {
                FilterCriteria filterUnion;
                if (OptimizeFiltersUnion(a[0], b[0], out filterUnion))
                {
                    union = new[] { filterUnion };
                    return true;
                }
            }

            union = null;
            return false;
        }

        public static bool OptimizeFiltersUnion(FilterCriteria a, FilterCriteria b, out FilterCriteria union)
        {
            if (a.Filter == b.Filter && a.Property == b.Property && a.Operation == b.Operation)
            {
                if (a.Value == b.Value)
                {
                    union = a;
                    return true;
                }
                else if (string.Equals(a.Operation, "In", StringComparison.OrdinalIgnoreCase)
                    && a.Value is IList<Guid> && b.Value is IList<Guid>)
                {
                    var listUnion = ((IList<Guid>)a.Value).Union((IList<Guid>)b.Value).ToList();
                    union = new FilterCriteria(a.Property, a.Operation, listUnion);
                    return true;
                }
                else if (string.Equals(a.Operation, "In", StringComparison.OrdinalIgnoreCase)
                    && a.Value is IList<Guid?> && b.Value is IList<Guid?>)
                {
                    var listUnion = ((IList<Guid?>)a.Value).Union((IList<Guid?>)b.Value).ToList();
                    union = new FilterCriteria(a.Property, a.Operation, listUnion);
                    return true;
                }
            }

            union = null;
            return false;
        }
    }
}
