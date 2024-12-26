// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.TestBench.Containers;
using Silverback.TestBench.Containers.Commands;
using Silverback.TestBench.Producer;
using Silverback.TestBench.UI.Windows;
using Silverback.TestBench.Utils;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Trace;

namespace Silverback.TestBench;

public partial class App
{
    private IHost? _host;

    private ILogger<App>? _logger;

    public Task StartHostAsync() => (_host ?? throw new InvalidOperationException("Host is not initialized.")).StartAsync();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices(
                (_, services) =>
                {
                    services
                        .AddSingleton(this)
                        .AddSerilog(Path.Combine(FileSystemHelper.LogsFolder, "testbench.log"));

                    AddUtils(services);
                    AddWindows(services);

                    AddProducer(services);
                    AddContainers(services);
                    AddLogsAndTrace(services);

                    services.AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddKafka().AddMqtt())
                        .AddBrokerClientsConfigurator<BrokerClientsConfigurator>();
                })
            .Build();

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Application started");

        _host.Services.GetRequiredService<InitWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application exiting");
        _host?.StopAsync().Wait(TimeSpan.FromSeconds(10));
        _host?.Dispose();

        base.OnExit(e);
    }

    private static void AddUtils(IServiceCollection services) =>
        services
            .AddSingleton<FileSystemHelper>()
            .AddSingleton<ExceptionHandler>();

    private static void AddWindows(IServiceCollection services) =>
        services
            .AddTransient<InitWindow>()
            .AddTransient<InitViewModel>()
            .AddTransient<MainWindow>()
            .AddSingleton<MainViewModel>();

    private static void AddProducer(IServiceCollection services) =>
        services
            .AddSingleton<MessagesTracker>()
            .AddTransient<KafkaTopicsCreator>()
            .AddSingleton<ProducerBackgroundService>()
            .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ProducerBackgroundService>());

    private static void AddContainers(IServiceCollection services) =>
        services
            .AddTransient<DockerImagesBuilder>()
            .AddSingleton<ContainersOrchestrator>()
            .AddHostedService<ContainersRandomScaler>()
            .ConfigureSilverback()
            .AddSingletonSubscriber<ContainersOrchestratorCommandsHandler>();

    private static void AddLogsAndTrace(IServiceCollection services) =>
        services
            .AddSingleton<LogsViewModel>()
            .AddSingleton<TraceViewModel>();
}
