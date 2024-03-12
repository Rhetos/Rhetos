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

namespace Rhetos.Extensibility
{
    internal sealed class CachedFileData : IEquatable<CachedFileData>
    {
        public string ModifiedTime { get; set; }
        public List<string> TypesWithExports { get; set; } = new List<string>();

        public bool Equals(CachedFileData other)
            => ModifiedTime == other.ModifiedTime
                && TypesWithExports.SequenceEqual(other.TypesWithExports);

        public override bool Equals(object obj)
            => obj is CachedFileData objCf && Equals(objCf);

        public override int GetHashCode()
            => HashCode.Combine(ModifiedTime);
    }

    internal sealed class PluginsCacheData
    {
        public Dictionary<string, CachedFileData> Assemblies { get; set; } = new Dictionary<string, CachedFileData>();
    }
}
