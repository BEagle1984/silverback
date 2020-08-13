// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Testing framework")]
    public class TestApplicationFactory : WebApplicationFactory<BlankStartup>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder
                        .UseStartup<BlankStartup>());
        }
    }
}
