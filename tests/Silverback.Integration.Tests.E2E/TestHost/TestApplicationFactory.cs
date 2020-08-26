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
        protected override IHostBuilder? CreateHostBuilder()
        {
            return null;
        }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder().UseStartup<BlankStartup>();
        }
    }
}
