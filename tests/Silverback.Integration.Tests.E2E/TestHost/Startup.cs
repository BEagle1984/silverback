// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.Integration.E2E.TestHost;

public class Startup
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called by IHost")]
    public void ConfigureServices(IServiceCollection services) => services.AddHealthChecks();

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called by IHost")]
    public void Configure(IApplicationBuilder app) => app.UseRouting().UseEndpoints(
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
