using Common.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Domain;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using SilverbackShop.Baskets.Domain;
using SilverbackShop.Baskets.Domain.Services;
using Swashbuckle.AspNetCore.Swagger;
using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Integration;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Baskets.Infrastructure;

namespace SilverbackShop.Baskets.Service
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BasketsDbContext>(o => o.UseSqlite($"Data Source={_configuration["DB:Path"]}Baskets.db"));

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
            services.AddScoped<CheckoutService>();
            services.AddScoped<InventoryService>();
            services.AddScoped<BasketsService>();
            services.AddScoped<ProductsService>();

            // Repositories
            services.AddScoped<IBasketsRepository>();
            services.AddScoped<IInventoryItemsRepository>();
            services.AddScoped<IProductsRepository>();

            // TODO: Can get rid of this?
            services.AddSingleton<OutboundConnector>();

            var serviceProvider = services.BuildServiceProvider();

            // TODO: Create extension method services.AddBus() in Silverback.AspNetCore
            var bus = new BusBuilder()
                .WithFactory(t => serviceProvider.GetService(t), t => serviceProvider.GetServices(t))
                .UseLogger(_loggerFactory)
                .Build()
                .ConfigureBroker<FileSystemBroker>(c => c.OnPath(_configuration["Broker:Path"]))
                .ConfigureUsing<BasketsDomainMessagingConfigurator>();

            services.AddSingleton(bus);
            services.AddSingleton(bus.GetEventPublisher<IDomainEvent<IDomainEntity>>());

            bus.ConnectBrokers();

            return serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.ReturnExceptionsAsJson();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Baskets API V1");
            });

            InitializeDatabase(app);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<BasketsDbContext>().Database.Migrate();
            }
        }
    }
}
