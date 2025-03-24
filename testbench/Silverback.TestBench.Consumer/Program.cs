// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.TestBench;
using Silverback.TestBench.Consumer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSerilog(Environment.GetEnvironmentVariable("LOG_PATH") ?? "consumer.log");

builder.Services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka().AddMqtt())
    .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()
    .AddSingletonSubscriber<Subscriber>()
    .WithLogLevels(
        levels => levels
            .SetLogLevel(
                IntegrationLogEvents.ProcessingConsumedMessageError.EventId,
                (ex, level) => ex is SimulatedFailureException ? LogLevel.Information : level)
            .SetLogLevel(
                IntegrationLogEvents.SequenceProcessingError.EventId,
                (ex, level) => ex is SimulatedFailureException ? LogLevel.Information : level));

WebApplication app = builder.Build();

app.MapControllers();

app.Run();
