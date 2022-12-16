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
    /// This exception denotes an error during validation of data vs business logic rules.
    /// These errors result from end user's incorrect usage of the application.
    /// It supports message localization with message parameters, and custom error metadata.
    /// </summary>
    [Serializable]
    public class UserException : RhetosException
    {
        private string _userMessage;

        /// <summary>
        /// Exception message for localization.
        /// It contains the original message from the <see cref="UserException(string, object[])"/> constructor.
        /// <para>
        /// The message may contain indexed placeholders ({0}, {1}, ...) to support localization,
        /// and it needs to be formated with the message parameters provided in <see cref="UserException(string, object[])"/> constructor,
        /// see <see cref="MessageParameters"/> property.
        /// For example, by calling <c>string.Format(userException.UserMessage, userException.MessageParameters)</c>.
        /// </para>
        /// <para>
        /// The base <see cref="Exception.Message"/> property contains the formatted string from <see cref="UserMessage"/>
        /// with the placeholders replaced with <see cref="MessageParameters"/>.
        /// </para>
        /// </summary>
        public string UserMessage { get => _userMessage ?? Message; set => _userMessage = value; } // This custom getter handles an edge case when the base Exception automatically sets the Message property if not explicitly provided.

        /// <summary>
        /// Additional error metadata, for example a property name that caused the error.
        /// </summary>
        public string SystemMessage { get; set; } // TODO: Remove this property and switch to RhetosException.Info property for error metadata.

        /// <summary>
        /// The <see cref="MessageParameters"/> are used with the <see cref="UserMessage"/> property, matching the arguments of the <c>string.Format(Message, MessageParameters)</c> method.
        /// </summary>
        public object[] MessageParameters { get; }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        public UserException() { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        public UserException(string message) : this(message, null, null, null) { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        /// <param name="systemMessage">
        /// Additional error metadata for example a property name that caused the error.
        /// </param>
        public UserException(string message, string systemMessage) : this(message, null, systemMessage, null) { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        /// <param name="inner">
        /// The exception that is the cause of the current exception.
        /// </param>
        public UserException(string message, Exception inner) : this(message, null, null, inner) { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        /// <param name="systemMessage">
        /// Additional error metadata for example a property name that caused the error.
        /// </param>
        /// <param name="inner">
        /// The exception that is the cause of the current exception.
        /// </param>
        public UserException(string message, string systemMessage, Exception inner) : this(message, null, systemMessage, inner) { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <remarks>
        /// This constructor enables user error message localization with parameters.
        /// The provided original message will be saved to <see cref="UserMessage"/> property,
        /// while the formatted message with replaced parameters will be saved to <see cref="Exception.Message"/> property.
        /// </remarks>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        /// <param name="messageParameters">
        /// Parameters for string <paramref name="message"/>, similar to <see cref="string.Format(string, object[])"/>.
        /// </param>
        public UserException(string message, object[] messageParameters) : this(message, messageParameters, null, null) { }

        /// <summary>
        /// An error during validation of data vs business logic rules. It supports localization with message parameters, and custom error metadata.
        /// </summary>
        /// <remarks>
        /// This constructor enables user error message localization with parameters.
        /// The provided original message will be saved to <see cref="UserMessage"/> property,
        /// while the formatted message with replaced parameters will be saved to <see cref="Exception.Message"/> property.
        /// </remarks>
        /// <param name="message">
        /// Error message for the end user.
        /// Use parameters similar to <see cref="string.Format(string, object[])"/> to simplify localization, for example "Value of {0} should be less than {1}.".
        /// Separating message parameters from the message allows a single translation to be used with different parameter values.
        /// </param>
        /// <param name="messageParameters">
        /// Parameters for string <paramref name="message"/>, similar to <see cref="string.Format(string, object[])"/>.
        /// </param>
        /// <param name="systemMessage">
        /// Additional error metadata for example a property name that caused the error.
        /// </param>
        /// <param name="inner">
        /// The exception that is the cause of the current exception.
        /// </param>
        public UserException(string message, object[] messageParameters, string systemMessage, Exception inner) : base(SimpleFormat(message, messageParameters), inner)
        {
            UserMessage = message;
            MessageParameters = messageParameters;
            SystemMessage = systemMessage;
        }

        private static string SimpleFormat(string message, object[] messageParameters)
        {
            if (message == null && messageParameters == null)
                return null; // Null value results with a standard default exception message.

            try
            {
                return string.Format(message, messageParameters ?? Array.Empty<object>());
            }
            catch (Exception e)
            {
                string messageReport = message != null ? $"\"{message}\"" : "null";

                string parametersReport;
                if (messageParameters == null)
                    parametersReport = "null";
                else if (messageParameters.Length == 0)
                    parametersReport = "no parameters";
                else
                    parametersReport = "\"" + string.Join(", ", messageParameters) + "\"";

                string error = $"Invalid error message format. Message: {messageReport}, Parameters: {parametersReport}.";
                if (e is FormatException formatException)
                    throw new ArgumentException(error + " " + formatException.Message);
                else
                    throw new ArgumentException(error, e);
            }
        }

        protected UserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(SystemMessage))
                return base.ToString();
            else
                return $"{base.ToString()}{Environment.NewLine}SystemMessage: {SystemMessage}";
        }
    }
}
