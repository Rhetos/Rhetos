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

namespace Rhetos
{
    /// <summary>
    /// Legacy exception allows backward compatible JSON response format (a string instead of an object).
    /// </summary>
    [Serializable]
    [Obsolete("Use ClientException instead.")]
    public class LegacyClientException : ClientException
    {
        public LegacyClientException() : base() { }
        public LegacyClientException(string message) : base(message) { }
        public LegacyClientException(string message, Exception inner) : base(message, inner) { }
        protected LegacyClientException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }

        public bool Severe { get; set; } = true;
    }
}
