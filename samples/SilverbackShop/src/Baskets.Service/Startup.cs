using Common.Api;
using Common.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Baskets.Domain.Services;
using SilverbackShop.Baskets.Infrastructure;
using SilverbackShop.Common.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

namespace SilverbackShop.Baskets.Service
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BasketsDbContext>(o =>
            {
                o.UseSqlServer(_configuration.GetConnectionString("BasketsDbContext").SetServerName());
                //o.UseSqlite($"Data Source={_configuration["DB:Path"]}Baskets.db");
            });

            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "SilverbackShop - Baskets API",
                    Version = "v1"
                });
            });

            // Domain Services
            services
                .AddScoped<CheckoutService>()
                .AddScoped<InventoryService>()
                .AddScoped<BasketsService>()
                .AddScoped<ProductsService>();

            // Repositories
            services
                .AddScoped<IBasketsRepository, BasketsRepository>()
                .AddScoped<IInventoryItemsRepository, InventoryItemsRepository>()
                .AddScoped<IProductsRepository, ProductsRepository>();

            // Bus
            services
                .AddBus()
                .AddScoped<ISubscriber, InventoryService>()
                .AddScoped<ISubscriber, ProductsService>()
                .AddScoped<ISubscriber, BasketEventsMapper>()
                .AddBroker<FileSystemBroker>(options => options
                    .SerializeAsJson()
                    .AddDbContextOutboundConnector()
                    .AddDbContextInboundConnector());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, BasketsDbContext basketsDbContext, IBrokerEndpointsConfigurationBuilder endpoints)
        {
            ConfigureRequestPipeline(app);

            basketsDbContext.Database.Migrate();

            var brokerBasePath = _configuration["Broker:Path"];

            endpoints
                .AddOutbound<IIntegrationEvent>(FileSystemEndpoint.Create("basket-events", brokerBasePath))
                .AddInbound(FileSystemEndpoint.Create("catalog-events", brokerBasePath))
                .Connect();
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
