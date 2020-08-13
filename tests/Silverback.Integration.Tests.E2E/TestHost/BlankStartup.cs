// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    public class BlankStartup
    {
        [SuppressMessage("", "CA1822", Justification = "Called by IHost")]
        [SuppressMessage("", "CA1801", Justification = "Called by IHost")]
        public void ConfigureServices(IServiceCollection services)
        {
        }

        [SuppressMessage("", "CA1822", Justification = "Called by IHost")]
        [SuppressMessage("", "CA1801", Justification = "Called by IHost")]
        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
