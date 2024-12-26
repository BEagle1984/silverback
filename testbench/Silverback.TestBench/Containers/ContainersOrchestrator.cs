// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Utils;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Containers;

namespace Silverback.TestBench.Containers;

public sealed class ContainersOrchestrator : IDisposable
{
    private const int InitialInstancesCount = 2;

    private readonly Dictionary<string, int> _containersCounters = [];

    private readonly MainViewModel _mainViewModel;

    private readonly MessagesTracker _messagesTracker;

    private readonly ILogger<ContainersOrchestrator> _logger;

    private readonly ILoggerFactory _loggerFactory;

    private readonly object _syncRoot = new();

    private bool _disposed;

    public ContainersOrchestrator(
        MainViewModel mainViewModel,
        MessagesTracker messagesTracker,
        ILoggerFactory loggerFactory)
    {
        _mainViewModel = mainViewModel;
        _messagesTracker = messagesTracker;
        _logger = loggerFactory.CreateLogger<ContainersOrchestrator>();
        _loggerFactory = loggerFactory;
    }

    public void InitDefaultInstances() => SetInstances(InitialInstancesCount);

    public void SetInstances(int count) => SetInstances(DockerImagesBuilder.DefaultImage, count);

    public void SetInstances(DockerImage dockerImage, int count)
    {
        if (_disposed)
            return;

        lock (_syncRoot)
        {
            ContainerInstanceViewModel[] existingContainers = GetExistingContainers(dockerImage.Name, dockerImage.Tag);

            if (existingContainers.Length < count)
                IncrementInstances(dockerImage, count, existingContainers);
            else if (existingContainers.Length > count)
                DecrementInstances(dockerImage, count, existingContainers);
        }
    }

    public void ScaleOut() => ScaleOut(DockerImagesBuilder.DefaultImage);

    public void ScaleOut(DockerImage dockerImage)
    {
        if (_disposed)
            return;

        lock (_syncRoot)
        {
            ContainerInstanceViewModel[] existingContainers = GetExistingContainers(dockerImage.Name, dockerImage.Tag);
            IncrementInstances(dockerImage, existingContainers.Length + 1, existingContainers);
        }
    }

    public void ScaleIn() => ScaleIn(DockerImagesBuilder.DefaultImage);

    public void ScaleIn(DockerImage dockerImage)
    {
        if (_disposed)
            return;

        lock (_syncRoot)
        {
            ContainerInstanceViewModel[] existingContainers = GetExistingContainers(dockerImage.Name, dockerImage.Tag);

            if (existingContainers.Length == 0)
                return;

            DecrementInstances(dockerImage, existingContainers.Length - 1, existingContainers);
        }
    }

    public void Dispose()
    {
        _disposed = true;

        lock (_syncRoot)
        {
            foreach (ContainerInstanceViewModel container in _mainViewModel.ContainerInstances)
            {
                StopContainer(container);
            }
        }
    }

    private static IContainerService StartContainer(string imageName, string imageTag, string containerName, bool isRetry = false)
    {
        try
        {
            return new Builder()
                .UseContainer()
                .UseImage($"{imageName}:{imageTag}")
                .WithName(containerName)
                .ExposePort(80)
                .WaitForPort("80/tcp", 10_000)
                .Mount(FileSystemHelper.LogsFolder, "/logs", MountType.ReadWrite)
                .WithEnvironment(
                    $"CONTAINER_NAME={containerName}",
                    $"LOG_PATH=/logs/{containerName}.log")
                .UseNetwork("silverback_default")
                .Build()
                .Start();
        }
        catch (FluentDockerException)
        {
            if (isRetry)
                throw;

            // Try stopping the container if it was already running
            new Hosts().Discover(true).FirstOrDefault()?
                .GetContainers().FirstOrDefault(container => container.Name == containerName)?
                .Dispose();

            return StartContainer(imageName, imageTag, containerName, true);
        }
    }

    private ContainerInstanceViewModel[] GetExistingContainers(string imageName, string imageTag) =>
        _mainViewModel.ContainerInstances
            .Reverse()
            .Where(
                container => container.ContainerService.Image.Name == imageName && container.ContainerService.Image.Tag == imageTag &&
                             container.Status == ContainerStatus.Running)
            .ToArray();

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Intentional to run in background")]
    private void IncrementInstances(DockerImage dockerImage, int count, ContainerInstanceViewModel[] existingContainers) =>
        Task.Run(
            () =>
            {
                _logger.LogInformation(
                    "Increasing {ImageName} instances from {CurrentCount} to {DesiredCount}",
                    dockerImage.Name,
                    existingContainers.Length,
                    count);

                for (int i = 0; i < count - existingContainers.Length; i++)
                {
                    string containerName = $"{dockerImage.Name}-{GetNextContainerIndex(dockerImage.Name)}";
                    IContainerService containerService = StartContainer(dockerImage.Name, dockerImage.Tag, containerName);

                    Application.Current.Dispatcher.Invoke(
                        () => _mainViewModel.ContainerInstances.Insert(
                            0,
                            new ContainerInstanceViewModel(containerService, _messagesTracker, _mainViewModel.Logs, _loggerFactory)));

                    _logger.LogInformation("Started container {ContainerName}", containerService.Name);
                }
            });

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Intentional to run in background")]
    private void DecrementInstances(DockerImage dockerImage, int count, ContainerInstanceViewModel[] existingContainers)
    {
        _logger.LogInformation(
            "Decrementing {ImageName} instances from {CurrentCount} to {DesiredCount}",
            dockerImage.Name,
            existingContainers.Length,
            count);

        foreach (ContainerInstanceViewModel container in existingContainers.Take(existingContainers.Length - count))
        {
            Task.Run(() => StopContainer(container));
        }
    }

    private int GetNextContainerIndex(string imageName)
    {
        _containersCounters.TryGetValue(imageName, out int currentCount);
        return _containersCounters[imageName] = currentCount + 1;
    }

    private void StopContainer(ContainerInstanceViewModel container)
    {
        container.SetStopping();
        container.ContainerService.Dispose();

        _logger.LogInformation("Stopped container {ContainerName}", container.ContainerService.Name);
    }
}
