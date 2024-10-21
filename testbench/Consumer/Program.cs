// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.TestBench.Consumer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging(
    loggingBuilder =>
    {
        loggingBuilder.AddSerilog(
            new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Silverback", LogEventLevel.Verbose)
                .WriteTo.File(
                    Environment.GetEnvironmentVariable("LOG_PATH") ?? "consumer.log",
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger());
    });

builder.Services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka().AddMqtt())
    .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()
    .AddSingletonSubscriber<Subscriber>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
