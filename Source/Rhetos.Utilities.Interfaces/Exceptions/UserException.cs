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
    [Serializable]
    public class UserException : RhetosException
    {
        public string SystemMessage { get; set; } // TODO: Remove this property and switch to RhetosException.Info property for error metadata.

        /// <summary>
        /// The MessageParameters are used with the Message property, matching the arguments of the string.Format(Message, MessageParameters) method.
        /// </summary>
        public object[] MessageParameters { get; }

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

        /// <summary>
        /// User error message localization with parameters.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Single message translation can be used in multiple scenarios with different parameter values.
        /// </param>
        /// <param name="messageParameters">
        /// Parameters for string <paramref name="message"/>, similar to <see cref="string.Format(string, object[])"/>.
        /// </param>
        public UserException(string message, object[] messageParameters) : base(message)
        {
            MessageParameters = messageParameters;
        }

        /// <summary>
        /// User error message localization with parameters, error metadata and inner exception.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Single message translation can be used in multiple scenarios with different parameter values.
        /// </param>
        /// <param name="messageParameters">
        /// Parameters for string <paramref name="message"/>, similar to <see cref="string.Format(string, object[])"/>.
        /// </param>
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

        /// <summary>
        /// Evaluates the message parameters with string.Format, without localization.
        /// Use this method in error logging to make sure every error is logged even if it's message format is not valid.
        /// </summary>
        public override string MessageForLog()
        {
            try
            {
                return string.Format(Message, MessageParameters ?? Array.Empty<object>());
            }
            catch (Exception e)
            {

                string parametersReport;
                if (MessageParameters == null)
                    parametersReport = "null";
                else if (MessageParameters.Length == 0)
                    parametersReport = "no parameters";
                else
                    parametersReport = "\"" + string.Join(", ", MessageParameters) + "\"";

                return $"Invalid error message format. Message: \"{Message ?? "null"}\"," +
                    $" Parameters: {parametersReport}," +
                    $" {e.GetType().Name}: {e.Message}";
            }
        }
    }
}
