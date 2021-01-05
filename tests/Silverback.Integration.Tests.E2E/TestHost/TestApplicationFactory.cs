// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    public class TestApplicationFactory : WebApplicationFactory<BlankStartup>
    {
        protected override IHostBuilder? CreateHostBuilder() =>
            null;

        protected override IWebHostBuilder CreateWebHostBuilder() =>
            WebHost
                .CreateDefaultBuilder()
                .UseDefaultServiceProvider(
                    (_, options) =>
                    {
                        options.ValidateScopes = true;
                        options.ValidateOnBuild = true;
                    })
                .UseStartup<BlankStartup>();
    }
}
