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
using System.Net;

namespace Rhetos
{
    /// <summary>
    /// Exception denotes a request or data format error occurred in communication between client and server. 
    /// Client is not adhering to server API.
    /// Web response HTTP status code on this exception is 400 by default, but can be configured by <see cref="HttpStatusCode"/>.
    /// </summary>
    [Serializable]
    public class ClientException : RhetosException
    {
        public ClientException() { }
        public ClientException(string message) : base(message) { }
        public ClientException(string message, Exception inner) : base(message, inner) { }
        protected ClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.BadRequest;
    }
}
