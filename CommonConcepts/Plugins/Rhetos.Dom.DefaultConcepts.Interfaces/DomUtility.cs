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
    public static class DomUtility
    {
        /// <summary>
        /// Standard GetHashCode() function in not guaranteed to return same result in different environments.
        /// </summary>
        public static int GetSubtypeImplementationHash(string implementationName)
        {
            if (string.IsNullOrEmpty(implementationName))
                return 0;

            const int seed = 1737350767;
            int hash = seed;
            foreach (char c in implementationName)
            {
                hash += c;
                hash *= seed;
            }
            return hash;
        }

        /// <summary>
        /// This functionality is also implemented in supertype's SQL view, see IsSubtypeOfInfo.GetSpecificImplementationId().
        /// </summary>
        public static Guid GetSubtypeImplementationId(Guid id, int subtypeImplementationHash)
        {
            if (subtypeImplementationHash == 0)
                return id;

            var guidBytes = id.ToByteArray();
            for (int b = 0; b < 4; b++)
                guidBytes[b] ^= (byte)(subtypeImplementationHash >> (24 - b * 8));
            return new Guid(guidBytes);
        }
    }
}
