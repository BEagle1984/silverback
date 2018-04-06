using System;
using Baskets.Domain;
using Baskets.Domain.Events;
using Baskets.Domain.Events.Handlers;
using Baskets.Domain.Model;
using Baskets.Domain.Repositories;
using Baskets.Domain.Services;
using Baskets.Infrastructure;
using Common.Data;
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
using Silverback.Messaging.Publishing;
using Swashbuckle.AspNetCore.Swagger;

namespace Baskets.Service
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
            services.AddDbContext<BasketsContext>(o => o.UseInMemoryDatabase("BasketsContext"));

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

            services.AddScoped<IBasketsRepository, BasketsRepository>();
            services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();

            services.AddTransient<BasketCheckoutEventHandler>();
            services.AddTransient<SimpleOutboundAdapter>();

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
            BrokersConfig.Instance.Add<FileSystemBroker>(c => c.OnPath(@"D:\Temp\Broker\SilverbackShop"));

            var bus = app.ApplicationServices.GetService<IBus>();
            bus.Config()
                .WithFactory(t => app.ApplicationServices.GetService(t))
                .Subscribe<BasketCheckoutEvent, BasketCheckoutEventHandler>()
                .AddOutbound<SimpleOutboundAdapter>(BasicEndpoint.Create("basket-events"));

            MessagesMappingsConfigurator.Configure(bus);

            // Init data
            var db = app.ApplicationServices.GetService<BasketsContext>();

            foreach (var stock in InventoryData.InitialStock)
                db.Add(InventoryItem.Create(stock.Item1, stock.Item2));

            db.SaveChanges();
        }
    }
}
