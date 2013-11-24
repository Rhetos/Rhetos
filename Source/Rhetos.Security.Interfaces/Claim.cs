/*
    Copyright (C) 2013 Omega software d.o.o.

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

        /// <summary>
        /// Resource and Right properties are case insensitive.
        /// </summary>
        public Claim(string resource, string right)
        {
            Resource = resource;
            Right = right;
        }

        public bool Equals(Claim other)
        {
            return other.Resource.Equals(Resource, StringComparison.OrdinalIgnoreCase)
                && other.Right.Equals(Right, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is Claim && ((Claim)obj).Resource.Equals(Resource, StringComparison.OrdinalIgnoreCase)
                && ((Claim)obj).Right.Equals(Right, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Resource.ToLower().GetHashCode() ^ Right.ToLower().GetHashCode();
        }

        public string FullName
        {
            get
            {
                return Resource + "." + Right;
            }
        }

        public bool Same(Claim other)
        {
            return other.Resource.Equals(Resource, StringComparison.Ordinal)
                && other.Right.Equals(Right, StringComparison.Ordinal);
        }
    }
}
