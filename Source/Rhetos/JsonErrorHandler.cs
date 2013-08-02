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
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web;
using System.ServiceModel.Dispatcher;
using System.Xml;
using Rhetos;
using Rhetos.Logging;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;

namespace Rhetos
{
    /// <summary>
    /// Converts exceptions to a HTTP WEB response that contains JSON-serialized string error message.
    /// Convenient for RESTful JSON web service.
    /// </summary>
    public class JsonErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return false;
        }

        public void ProvideFault(
            Exception error,
            MessageVersion version,
            ref Message fault)
        {
            if (error is WebFaultException)
                return;

            object responseData = error.GetType().Name + ": " + error.Message;
            var responseStatusCode = error is UserException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;

            fault = Message.CreateMessage(version, "", responseData,
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(responseData.GetType()));

            fault.Properties.Add(WebBodyFormatMessageProperty.Name,
                new WebBodyFormatMessageProperty(WebContentFormat.Json));

            fault.Properties.Add(HttpResponseMessageProperty.Name,
                new HttpResponseMessageProperty {StatusCode = responseStatusCode});

            var response = WebOperationContext.Current.OutgoingResponse;
            response.ContentType = "application/json";
            response.StatusCode = responseStatusCode;
        }
    }
}