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

namespace Rhetos.Dom.DefaultConcepts
{
    public class HierarchyCircularReferenceException : Exception { }

    public class HierarchyInfo // TODO: Rename to HierarchyIndexes
    {
        public Guid ID;
        public int LeftIndex;
        public int RightIndex;
        public int Level;
        public string Path;

        /// <summary>
        /// If pathSeparator argument is null, the Path property will not be computed.
        /// </summary>
        public static HierarchyInfo[] Compute(HierarchyItem[] items, string pathSeparator)
        {
            if (items.Length == 0)
                return new HierarchyInfo[] { };

            var roots = items.Where(item => !item.ParentID.HasValue).ToArray();
            if (roots.Count() == 0)
                throw new HierarchyCircularReferenceException();

            var children = items.Where(item => item.ParentID.HasValue)
                .GroupBy(item => item.ParentID.Value)
                .ToDictionary(group => group.Key, group => group.ToArray());

            var result = items.Select(item => new HierarchyInfo { ID = item.ID }).ToArray();
            var index = 1;
            foreach (var root in roots)
                FillIndexes(root, 0, ref index, null, children, result.ToDictionary(i => i.ID), pathSeparator);
            return result;
        }

        private static void FillIndexes(HierarchyItem currentItem, int level, ref int index, string parentPath,
            IDictionary<Guid, HierarchyItem[]> children, IDictionary<Guid, HierarchyInfo> infos, string pathSeparator)
        {
            var currentInfo = infos[currentItem.ID];
            if (currentInfo.LeftIndex != 0)
                throw new HierarchyCircularReferenceException();

            currentInfo.LeftIndex = index++;
            currentInfo.Level = level;
            if (pathSeparator != null)
                if (parentPath == null)
                    currentInfo.Path = currentItem.Name ?? "";
                else
                    currentInfo.Path = parentPath + pathSeparator + (currentItem.Name ?? "");

            HierarchyItem[] currentChildren;
            children.TryGetValue(currentItem.ID, out currentChildren);
            if (currentChildren != null)
                foreach (var child in currentChildren)
                    FillIndexes(child, level + 1, ref index, currentInfo.Path, children, infos, pathSeparator);

            currentInfo.RightIndex = index++;
        }
    }
}
