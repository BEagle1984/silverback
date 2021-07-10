// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    public class Startup
    {
        [SuppressMessage("", "CA1822", Justification = "Called by IHost")]
        [SuppressMessage("", "CA1801", Justification = "Called by IHost")]
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
        }

        [SuppressMessage("", "CA1822", Justification = "Called by IHost")]
        [SuppressMessage("", "CA1801", Justification = "Called by IHost")]
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting().UseEndpoints(
                endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                    endpoints.MapHealthChecks(
                        "/health1",
                        new HealthCheckOptions
                            { Predicate = registration => registration.Tags.Contains("1") });
                    endpoints.MapHealthChecks(
                        "/health2",
                        new HealthCheckOptions
                            { Predicate = registration => registration.Tags.Contains("2") });
                });
        }
    }
}
