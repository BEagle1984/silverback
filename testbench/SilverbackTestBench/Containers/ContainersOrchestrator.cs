// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Configuration.Models;
using Silverback.TestBench.Containers.Models;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Utils;

namespace Silverback.TestBench.Containers;

public sealed class ContainersOrchestrator : IDisposable
{
    private readonly List<IContainerService> _containers = new();

    private readonly Dictionary<string, int> _containersCounters = new();

    private readonly MessagesTracker _messagesTracker;

    private readonly ILogger<ContainersOrchestrator> _logger;

    private readonly LinkedList<ContainerLogParser> _containerLogParsers = new();

    private bool _disposed;

    public ContainersOrchestrator(MessagesTracker messagesTracker, ILogger<ContainersOrchestrator> logger)
    {
        _messagesTracker = messagesTracker;
        _logger = logger;
    }

    public static string GetLogFilePath(string containerName) => Path.Combine(FileSystemHelper.LogsFolder, $"{containerName}.log");

    public void InitDefaultInstances()
    {
        foreach (DockerContainer dockerContainer in DockerContainers.GetAll())
        {
            SetInstances(dockerContainer, dockerContainer.DefaultInstances);
        }
    }

    public void SetInstances(DockerContainer dockerContainer, int count) => SetInstances(dockerContainer.ImageName, dockerContainer.ImageTag, count);

    public void SetInstances(string imageName, string imageTag, int count)
    {
        if (_disposed)
            return;

        lock (_containers)
        {
            IContainerService[] existingContainers = GetExistingContainers(imageName, imageTag);

            if (existingContainers.Length < count)
                IncrementInstances(imageName, imageTag, count, existingContainers);
            else if (existingContainers.Length > count)
                DecrementInstances(imageName, count, existingContainers);
        }
    }

    public void ScaleOut(DockerContainer dockerContainer) => ScaleOut(dockerContainer.ImageName, dockerContainer.ImageTag);

    public void ScaleOut(string imageName, string imageTag)
    {
        if (_disposed)
            return;

        lock (_containers)
        {
            IContainerService[] existingContainers = GetExistingContainers(imageName, imageTag);
            IncrementInstances(imageName, imageTag, existingContainers.Length + 1, existingContainers);
        }
    }

    public void ScaleIn(DockerContainer dockerContainer) => ScaleIn(dockerContainer.ImageName, dockerContainer.ImageTag);

    public void ScaleIn(string imageName, string imageTag)
    {
        if (_disposed)
            return;

        lock (_containers)
        {
            IContainerService[] existingContainers = GetExistingContainers(imageName, imageTag);

            if (existingContainers.Length == 0)
                return;

            DecrementInstances(imageName, existingContainers.Length - 1, existingContainers);
        }
    }

    public IReadOnlyCollection<ContainerStats> GetContainerStats() => _containerLogParsers.Select(parser => parser.Statistics).ToArray();

    public void Dispose()
    {
        _disposed = true;

        lock (_containers)
        {
            foreach (ContainerLogParser containerLogParser in _containerLogParsers)
            {
                containerLogParser.Dispose();
            }

            _containerLogParsers.Clear();

            _containers.ForEach(container => container.Dispose());
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
                .WithEnvironment($"CONTAINER_NAME={containerName}")
                .WithEnvironment($"LOG_PATH=/logs/{containerName}.log")
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

    private IContainerService[] GetExistingContainers(string imageName, string imageTag) => _containers.Where(container => container.Image.Name == imageName && container.Image.Tag == imageTag).ToArray();

    private void IncrementInstances(string imageName, string imageTag, int count, IContainerService[] existingContainers)
    {
        _logger.LogInformation(
            "Increasing {ImageName} instances from {CurrentCount} to {DesiredCount}",
            imageName,
            existingContainers.Length,
            count);

        for (int i = 0; i < count - existingContainers.Length; i++)
        {
            string containerName = $"{imageName}-{GetNextContainerIndex(imageName)}";
            IContainerService containerService = StartContainer(imageName, imageTag, containerName);

            _containers.Add(containerService);
            _containerLogParsers.AddFirst(new ContainerLogParser(containerService, _messagesTracker, _logger));

            _logger.LogInformation("Started container {ContainerName}", containerService.Name);
        }
    }

    private void DecrementInstances(string imageName, int count, IContainerService[] existingContainers)
    {
        _logger.LogInformation(
            "Decrementing {ImageName} instances from {CurrentCount} to {DesiredCount}",
            imageName,
            existingContainers.Length,
            count);

        foreach (IContainerService containerService in existingContainers.Take(existingContainers.Length - count))
        {
            containerService.Dispose();
            _containers.Remove(containerService);
            _logger.LogInformation("Stopped container {ContainerName}", containerService.Name);
        }
    }

    private int GetNextContainerIndex(string imageName)
    {
        _containersCounters.TryGetValue(imageName, out int currentCount);
        return _containersCounters[imageName] = currentCount + 1;
    }
}
