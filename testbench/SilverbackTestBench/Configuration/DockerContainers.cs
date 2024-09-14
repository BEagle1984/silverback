// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Configuration;

public static class DockerContainers
{
    public static DockerContainer Consumer { get; } = new("Consumer", "silverback-testbench-consumer", "latest", 2);

    public static DockerContainer[] GetAll() =>
        typeof(DockerContainers).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(property => (DockerContainer)(property.GetValue(null) ?? throw new InvalidOperationException("Null return value from docker container property.")))
            .ToArray();
}
