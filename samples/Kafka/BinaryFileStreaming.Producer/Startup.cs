using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Producer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable Silverback
            services
                .AddSilverback()

                // Use Apache Kafka as message broker
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka())

                // Delegate the inbound/outbound endpoints configuration to a separate
                // class.
                .AddEndpointsConfigurator<EndpointsConfigurator>();

            // Add API controllers and SwaggerGen
            services.AddControllers();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Enable middlewares to serve generated Swagger JSON and UI
            app.UseSwagger().UseSwaggerUI(
                uiOptions =>
                {
                    uiOptions.SwaggerEndpoint(
                        "/swagger/v1/swagger.json",
                        $"{GetType().Assembly.FullName} API");
                });

            // Enable routing and endpoints for controllers
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
