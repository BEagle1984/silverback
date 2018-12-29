using System;
using Common.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Catalog.Domain.Services;
using SilverbackShop.Catalog.Infrastructure;
using SilverbackShop.Catalog.Infrastructure.Repositories;
using SilverbackShop.Catalog.Service.Queries;
using SilverbackShop.Common.Infrastructure;
using SilverbackShop.Common.Infrastructure.Jobs;
using Swashbuckle.AspNetCore.Swagger;

namespace SilverbackShop.Catalog.Service
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<CatalogDbContext>(o =>
            {
                o.UseSqlServer(_configuration.GetConnectionString("CatalogDbContext").SetServerName());
                //o.UseSqlite($"Data Source={_configuration["DB:Path"]}Catalog.db");
            });

            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "SilverbackShop - Catalog API",
                    Version = "v1"
                });
            });

            // Repositories & Query Objects
            services.AddScoped<IProductsRepository, ProductsRepository>();
            services.AddScoped<IProductsQueries, ProductsQueries>();

            // Configure bus
            services
                .AddBus()
                .AddScoped<ISubscriber, ProductEventsMapper>()
                .AddBroker<FileSystemBroker>(options => options
                    .SerializeAsJson()
                    .AddDbOutboundConnector<CatalogDbContext>()
                    .AddDbOutboundWorker<CatalogDbContext>());

            services.AddSingleton<JobScheduler>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, CatalogDbContext catalogDbContext, IBrokerEndpointsConfigurationBuilder endpoints, JobScheduler jobScheduler)
        {
            ConfigureRequestPipeline(app);

            catalogDbContext.Database.Migrate();

            endpoints
                .AddOutbound<IIntegrationEvent>(
                    FileSystemEndpoint.Create("catalog-events", _configuration["Broker:Path"]))
                .Connect();

            jobScheduler.AddJob("outbound-queue-worker", TimeSpan.FromMilliseconds(100),
                s => s.GetRequiredService<OutboundQueueWorker>().ProcessQueue());

            // Configure outbound worker
        }

        private static void ConfigureRequestPipeline(IApplicationBuilder app)
        {
            app.ReturnExceptionsAsJson();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Baskets API V1");
            });
        }
    }
}
