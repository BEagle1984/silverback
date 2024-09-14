// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Ductus.FluentDocker.Services;

namespace Silverback.TestBench.Containers.Models;

public class ContainerStats
{
    public ContainerStats(IContainerService containerService)
    {
        ContainerService = containerService;
    }

    public IContainerService ContainerService { get; }

    public int ProcessedMessagesCount { get; set; }

    public int ErrorsCount { get; set; }

    public int WarningsCount { get; set; }

    public int FatalErrorsCount { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Stopped { get; set; }
}
