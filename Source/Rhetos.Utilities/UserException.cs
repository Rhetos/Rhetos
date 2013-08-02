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

namespace Rhetos
{
    [global::System.Serializable]
    public class UserException : Exception
    {
        public string SystemMessage;
        public UserException() { }
        public UserException(string message) : base(message) { }
        public UserException(string message, string systemMessage) : base(message) { SystemMessage = systemMessage; }
        public UserException(string message, Exception inner) : base(message, inner) { }
        public UserException(string message, string systemMessage, Exception inner) : base(message, inner) { SystemMessage = systemMessage; }
        protected UserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

}
