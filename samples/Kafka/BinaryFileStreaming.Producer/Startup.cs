// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Samples.BinaryFileStreaming.Producer
{
    public class Startup
    {
        [SuppressMessage("", "CA1822", Justification = "Framework convention")]
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable Silverback
            services
                .AddSilverback()

                // Use Apache Kafka as message broker
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka())

                // Delegate the inbound/outbound endpoints configuration to a separate type
                .AddEndpointsConfigurator<EndpointsConfigurator>();

            // Add API controllers and SwaggerGen
            services.AddControllers();
            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            // Enable middlewares to serve generated Swagger JSON and UI
            app.UseSwagger().UseSwaggerUI(
                uiOptions =>
                {
                    uiOptions.SwaggerEndpoint("/swagger/v1/swagger.json", $"{GetType().Assembly.FullName} API");
                });

            // Enable routing and endpoints for controllers
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
