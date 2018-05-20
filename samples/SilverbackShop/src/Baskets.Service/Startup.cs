using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Domain;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using SilverbackShop.Baskets.Domain;
using SilverbackShop.Baskets.Domain.Model;
using SilverbackShop.Baskets.Domain.Repositories;
using SilverbackShop.Baskets.Domain.Services;
using SilverbackShop.Baskets.Domain.Subscribers;
using SilverbackShop.Baskets.Infrastructure;
using SilverbackShop.Common.Data;
using Swashbuckle.AspNetCore.Swagger;

namespace SilverbackShop.Baskets.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BasketsContext>(o => o.UseSqlite($"Data Source={Environment.GetEnvironmentVariable("DB_PATH")}Baskets.db"));

            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "SilverbackShop - Baskets API",
                    Version = "v1"
                });
            });

            services.AddScoped<CheckoutService>();
            services.AddScoped<InventoryService>();
            services.AddScoped<BasketsService>();

            services.AddScoped<IBasketsUnitOfWork, BasketsUnitOfWork>();

            services.AddTransient<ISubscriber, InventoryMultiSubscriber>();
            services.AddTransient<ISubscriber, CatalogMultiSubscriber>();

            // TODO: Can get rid of this?
            services.AddSingleton<SimpleOutboundAdapter>();

            // TODO: Create extension method services.AddBus() in Silverback.AspNetCore
            var bus = new Bus();
            services.AddSingleton<IBus>(bus);
            services.AddSingleton(bus.GetEventPublisher<IDomainEvent<IDomainEntity>>());

            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Baskets API V1");
            });

            // TODO: Create extension method app.UseBus() in Silverback.AspNetCore
            var bus = app.ApplicationServices.GetService<IBus>();
            bus.Config()
                .ConfigureBroker<FileSystemBroker>(c => c.OnPath(Environment.GetEnvironmentVariable("BROKER_PATH")))
                .WithFactory(t => app.ApplicationServices.GetService(t), t => app.ApplicationServices.GetServices(t))
                .ConfigureUsing<BasketsDomainMessagingConfigurator>()
                .AutoSubscribe()
                .ConnectBrokers();

            InitializeDatabase(app);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<BasketsContext>().Database.Migrate();
            }
        }
    }
}
