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

using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rhetos;
using Rhetos.Security;
using Rhetos.Utilities;

IHostBuilder builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((context, services) =>
{
    services.AddRhetosHost((IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder) =>
    {
        rhetosHostBuilder
            .ConfigureRhetosAppDefaults()
            .ConfigureConfiguration(configurationBuilder => configurationBuilder
                .AddJsonFile("local.settings.json")
                .MapNetCoreConfiguration(context.Configuration))
            .ConfigureContainer(builder =>
            {
                builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
            });
    });
});

IHost host = builder.Build();

var rhetosAppOptions = host.Services.GetService<IRhetosComponent<RhetosAppOptions>>()?.Value;
Console.WriteLine($"Running Rhetos app '{rhetosAppOptions?.RhetosAppAssemblyFileName}'.");
Console.WriteLine($"Generated classes in assembly '{typeof(Bookstore.Book).Assembly.GetName().Name}'.");

Console.WriteLine("Executing test query:");
var repository = host.Services.GetService<IRhetosComponent<Common.DomRepository>>().Value;
var claims = repository.Common.Claim.Query().Take(5).Select(c => c.ClaimResource + "/" + c.ClaimRight);
Console.WriteLine(claims.ToQueryString());
Console.WriteLine(string.Join(Environment.NewLine, claims));
