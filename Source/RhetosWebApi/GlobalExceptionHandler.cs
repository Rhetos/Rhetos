using Autofac.Integration.WebApi;
using Rhetos;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Net.Http;
using System.Net;
using Rhetos.Utilities;
using Newtonsoft.Json.Linq;

namespace RhetosWebApi
{
    public class ResponseMessage : IHttpActionResult
    {
        public HttpRequestMessage Request { get; set; }

        public HttpStatusCode ResponseStatusCode;

        public string UserMessage;
        public string SystemMessage;

        public string Content { get; set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response =
                             new HttpResponseMessage(ResponseStatusCode);
            string summary = Content == null 
                ? "{\"SystemMessage\": \"" + (SystemMessage ?? "<null>") + "\", \"UserMessage\": \"" + (UserMessage ?? "<null>") + "\"}" 
                : "{\"Message\":\"" + Content + "\"}";

            response.Content = new StringContent(summary);
            response.RequestMessage = Request;
            return Task.FromResult(response);
        }

        public override string ToString()
        {
            return "SystemMessage: " + (SystemMessage ?? "<null>") + ", UserMessage: " + (UserMessage ?? "<null>");
        }
    }

    public class GlobalExceptionHandler : IExceptionHandler
    {

        public async Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var dependencyResolver = (AutofacWebApiDependencyResolver)GlobalConfiguration.Configuration.DependencyResolver;
            var container = dependencyResolver.Container;

            var logProvider = Autofac.ResolutionExtensions.Resolve<ILogProvider>(container);
            var Logger = logProvider.GetLogger("GlobalExceptionHandler");

            Exception error = context.Exception;
            ResponseMessage response;

            if (error is UserException)
            {
                Logger.Trace(() => error.ToString());
                var userError = (UserException)error;
                var localizer = Autofac.ResolutionExtensions.Resolve<ILocalizer>(container);
                response = new ResponseMessage
                {
                    UserMessage = localizer[userError.Message, userError.MessageParameters],
                    SystemMessage = userError.SystemMessage,
                    ResponseStatusCode = HttpStatusCode.BadRequest
                };
            }
            else if (error is LegacyClientException)
            {
                response = new ResponseMessage
                {
                    ResponseStatusCode = ((LegacyClientException)error).HttpStatusCode,
                    Content = error.Message
                };
            }
            else if (error is ClientException)
            {
                response = new ResponseMessage
                {
                    SystemMessage = error.Message,
                    ResponseStatusCode = HttpStatusCode.BadRequest
                };
            }
            else
            {
                response = new ResponseMessage
                {
                    SystemMessage = "Internal server error occurred (" + error.GetType().Name + "). See RhetosServer.log for more information.",
                    ResponseStatusCode = HttpStatusCode.InternalServerError
                };
            }

            context.Result = response;
        }
    }
}