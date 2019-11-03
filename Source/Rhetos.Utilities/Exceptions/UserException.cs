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
    /// This exceptions denotes an error during validation of data vs business logic rules.
    /// These errors result from end user's incorrect usage of the application.
    /// Web response HTTP status code on this exception is 400.
    /// </summary>
    [global::System.Serializable]
    public class UserException : RhetosException
    {
        public string SystemMessage; // TODO: Remove this property and switch to RhetosException.Info property for error metadata.

        /// <summary>
        /// The MessageParameters are used with the Message property, matching the arguments of the string.Format(Message, MessageParameters) method.
        /// </summary>
        public readonly object[] MessageParameters;

        public UserException() { }

        public UserException(string message) : base(message) { }

        public UserException(string message, string systemMessage) : base(message)
        {
            SystemMessage = systemMessage;
        }

        public UserException(string message, Exception inner) : base(message, inner) { }

        public UserException(string message, string systemMessage, Exception inner) : base(message, inner)
        {
            SystemMessage = systemMessage;
        }

        public UserException(string message, object[] messageParameters, string systemMessage, Exception inner) : base(message, inner)
        {
            MessageParameters = messageParameters;
            SystemMessage = systemMessage;
        }

        protected UserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string ToString()
        {
            return base.ToString()
                + "\r\nMessageParameters: " + (MessageParameters != null ? string.Join(", ", MessageParameters) : "null")
                + "\r\nSystemMessage: " + SystemMessage;
        }
    }
}
