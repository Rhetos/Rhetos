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

namespace Rhetos
{
    /// <summary>
    /// Base class for Rhetos framework and application exceptions.
    /// </summary>
    [Serializable]
    public abstract class RhetosException : Exception
    {
        /// <summary>
        /// Additional error context. It will be sent to the client if the exception is UserException or ClientException.
        /// </summary>
        /// <remarks>
        /// The Info property is used instead of the existing "Data" dictionary to avoid security issue when sending data to the client,
        /// since other tools might use the Data for internal debugging data.
        /// </remarks>
        public IDictionary<string, object> Info { get; set; } = new Dictionary<string, object>();

        protected RhetosException() { }
        protected RhetosException(string message) : base(message) { }
        protected RhetosException(string message, Exception inner) : base(message, inner) { }
        protected RhetosException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string ToString()
        {
            string infoReport;
            if (Info?.Any() == true)
                infoReport = "\r\nInfo: " + string.Join(", ", Info.Select(info => $"{info.Key}={info.Value}"));
            else
                infoReport = "";

            return base.ToString() + infoReport;
        }

        public virtual string MessageForLog()
        {
            return Message;
        }
    }
}
