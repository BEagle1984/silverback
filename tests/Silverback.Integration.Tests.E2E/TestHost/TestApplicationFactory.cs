// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Silverback.Tests.Integration.E2E.TestHost;

public class TestApplicationFactory : WebApplicationFactory<Startup>
{
    protected override IHostBuilder CreateHostBuilder() =>
        new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseDefaultServiceProvider((_, options) =>
                    {
                        options.ValidateScopes = true;
                        options.ValidateOnBuild = true;
                    })
                    .UseStartup<Startup>();
            });
}
