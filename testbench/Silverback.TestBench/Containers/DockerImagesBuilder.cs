// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ductus.FluentDocker.Builders;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Utils;

namespace Silverback.TestBench.Containers;

public class DockerImagesBuilder
{
    private readonly ILogger<DockerImagesBuilder> _logger;

    public DockerImagesBuilder(ILogger<DockerImagesBuilder> logger)
    {
        _logger = logger;
    }

    public static IReadOnlyList<DockerImage> Images { get; } =
    [
        new("Silverback.TestBench.Consumer", "silverback-testbench-consumer", "latest")
    ];

    public static DockerImage DefaultImage => Images[0];

    public void BuildAll()
    {
        foreach (DockerImage image in Images)
        {
            Build(image.ProjectName, image.Name);
        }
    }

    public void Build(string projectName, string imageName)
    {
        string dockerFile = Path.Combine(FileSystemHelper.SolutionRoot, projectName, "Dockerfile");

        Stopwatch stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Building {ImageName} Docker image", imageName);

        new Builder()
            .DefineImage(imageName)
            .FromFile(dockerFile)
            .WorkingFolder(Path.Combine(Path.GetDirectoryName(dockerFile)!, "../.."))
            .Build().Start();

        _logger.LogInformation("Docker image {ImageName} built in {Elapsed}", imageName, stopwatch.Elapsed);
    }
}
