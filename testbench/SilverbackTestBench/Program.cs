// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.TestBench;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Producer;
using Silverback.TestBench.UI;
using Silverback.TestBench.Utils;
using Terminal.Gui;

await ParseArgsAsync(args);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        services =>
        {
            services
                .AddSerilog(Path.Combine(FileSystemHelper.LogsFolder, "testbench.log"));

            services.AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka().AddMqtt())
                .AddBrokerClientsConfigurator<BrokerClientsConfigurator>();

            services
                .AddSingleton<ContainersOrchestrator>()
                .AddSingleton<ContainersRandomScaling>()
                .AddSingleton<MessagesTracker>()
                .AddSingleton<ProducerBackgroundService>()
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ProducerBackgroundService>());

            services
                .AddSingleton<TestBenchApplication>()
                .AddSingleton<TestBenchTopLevel>()
                .AddSingleton<OverviewTopLevel>();
        })
    .Build();
Console.CancelKeyPress += (_, args) =>
{
    Application.RequestStop();
    args.Cancel = true;
};

Console.Write("Starting host...");
await host.StartAsync();
ConsoleHelper.WriteDone();

Console.Write("Starting containers...");
host.Services.GetRequiredService<ContainersOrchestrator>().InitDefaultInstances();
ConsoleHelper.WriteDone();

Console.WriteLine("-> running application");
host.Services.GetRequiredService<TestBenchApplication>().Run();

Console.Write("Stopping host...");
await host.StopAsync();
host.Dispose();
ConsoleHelper.WriteDone();

static async Task ParseArgsAsync(string[] strings)
{
    if (strings.Contains("--clear-logs") || strings.Contains("-c"))
        FileSystemHelper.ClearLogsFolder();

    if (strings.Contains("--build") || strings.Contains("-b"))
        DockerImagesBuilder.BuildAll();

    if (strings.Contains("--topics") || strings.Contains("-t"))
        await KafkaTopicsCreator.RecreateAllTopicsAsync();
}
