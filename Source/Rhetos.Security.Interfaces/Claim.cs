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

namespace Rhetos.Security
{
    /// <summary>
    /// Resource and Right properties are case insensitive.
    /// </summary>
    public class Claim : IEquatable<Claim>
    {
        public readonly string Resource;
        public readonly string Right;

        public static int EquivalentComparer(string resource1, string right1, string resource2, string right2)
        {
            var result = string.Compare(resource1, resource2, StringComparison.OrdinalIgnoreCase);
            if (result == 0)
                result = string.Compare(right1, right2, StringComparison.OrdinalIgnoreCase);
            return result;
        }

        public static int EquivalentHashCode(string resource, string right)
        {
            return resource.ToLower().GetHashCode() ^ right.ToLower().GetHashCode();
        }

        public Claim(string resource, string right)
        {
            Resource = resource;
            Right = right;
        }

        public bool Equals(Claim other)
        {
            return EquivalentComparer(Resource, Right, other.Resource, other.Right) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is Claim && Equals((Claim)obj);
        }

        public override int GetHashCode()
        {
            return EquivalentHashCode(Resource, Right);
        }

        public string FullName
        {
            get
            {
                return Resource + "." + Right;
            }
        }
    }
}
