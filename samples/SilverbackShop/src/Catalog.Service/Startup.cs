using Common.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Domain;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Messages;
using SilverbackShop.Catalog.Domain;
using SilverbackShop.Catalog.Domain.Repositories;
using SilverbackShop.Catalog.Infrastructure;
using SilverbackShop.Catalog.Infrastructure.Repositories;
using SilverbackShop.Catalog.Service.Queries;
using SilverbackShop.Common.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

namespace SilverbackShop.Catalog.Service
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        public Startup(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _loggerFactory = loggerFactory;
            _configuration = configuration;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
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

            // TODO: Can get rid of this?
            services.AddSingleton<OutboundConnector>();

            // TODO: Create extension method services.AddBus() in Silverback.AspNetCore
            services.AddSingleton(serviceProvider => new BusBuilder()
                .WithFactory(serviceProvider.GetService, serviceProvider.GetServices)
                .UseLogger(_loggerFactory)
                .Build()
                .ConfigureBroker<FileSystemBroker>(c => c.OnPath(_configuration["Broker:Path"]))
                .ConfigureUsing<CatalogDomainMessagingConfigurator>());
            services.AddSingleton(serviceProvider => serviceProvider.GetService<IBus>().GetEventPublisher<IEvent>());
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

            // TODO: Create extension method app.ConnectBrokers();
            app.ApplicationServices.GetService<IBus>().ConnectBrokers();
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
            }
        }
    }
}
