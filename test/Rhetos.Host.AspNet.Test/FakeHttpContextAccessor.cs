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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Host.AspNet.Test
{
    internal class FakeHttpContextAccessor : IHttpContextAccessor
    {
        private readonly FakeHttpContext _httpContext;

        public FakeHttpContextAccessor(string username, string ip, int? port)
        {
            var fakePrincipal = new ClaimsPrincipal(new FakeIdentity
            {
                AuthenticationType = "FakeAuthentication",
                IsAuthenticated = !string.IsNullOrEmpty(username),
                Name = username
            });
            var fakeConnection = !string.IsNullOrEmpty(ip)
                    ? new FakeConnectionInfo
                    {
                        RemoteIpAddress = IPAddress.Parse(ip),
                        RemotePort = port.Value
                    }
                    : null;
            _httpContext = new FakeHttpContext
            {
                User = fakePrincipal,
                ConnectionOverride = fakeConnection
            };
        }

        public HttpContext HttpContext { get => _httpContext; set => throw new System.NotImplementedException(); }
    }

    public class FakeHttpContext : HttpContext
    {
        public override IFeatureCollection Features => throw new NotImplementedException();
        public override HttpRequest Request => throw new NotImplementedException();
        public override HttpResponse Response => throw new NotImplementedException();
        public ConnectionInfo ConnectionOverride { get; set; }
        public override ConnectionInfo Connection => ConnectionOverride;
        public override WebSocketManager WebSockets => throw new NotImplementedException();
        public override ClaimsPrincipal User { get; set; }
        public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Abort() => throw new NotImplementedException();
    }

    public class FakeIdentity : IIdentity
    {
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Name { get; set; }
    }

    public class FakeConnectionInfo : ConnectionInfo
    {
        public override string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IPAddress RemoteIpAddress { get; set; }
        public override int RemotePort { get; set; }
        public override IPAddress LocalIpAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int LocalPort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override X509Certificate2 ClientCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}