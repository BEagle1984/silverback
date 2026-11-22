// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Silverback.Tests.Extended.TestBench.Utils;

namespace Silverback.Tests.Extended.TestBench.Containers;

public class DockerImagesBuilder
{
    private const int MaxErrorOutputLines = 50;

    private readonly ILogger<DockerImagesBuilder> _logger;

    public DockerImagesBuilder(ILogger<DockerImagesBuilder> logger)
    {
        _logger = logger;
    }

    public static IReadOnlyList<DockerImage> Images { get; } =
    [
        new("Silverback.Tests.Extended.TestBench.Consumer", "silverback-testbench-consumer", "latest")
    ];

    public static DockerImage DefaultImage => Images[0];

    public void BuildAll()
    {
        foreach (DockerImage image in Images)
        {
            Build(image);
        }
    }

    public void Build(DockerImage image)
    {
        string dockerFile = Path.Combine(FileSystemHelper.SolutionRoot, image.ProjectName, "Dockerfile");

        Stopwatch stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Building {ImageName} Docker image", image.Name);

        // Built with the Docker CLI because the base images must always be pulled (a stale cached SDK image doesn't
        // satisfy the SDK version required by global.json), and the FluentDocker image builder ignores AlwaysPull()
        using Process process = new();
        process.StartInfo.FileName = "docker";
        process.StartInfo.ArgumentList.Add("build");
        process.StartInfo.ArgumentList.Add("--pull");
        process.StartInfo.ArgumentList.Add("--progress=plain");
        process.StartInfo.ArgumentList.Add("--tag");
        process.StartInfo.ArgumentList.Add($"{image.Name}:{image.Tag}");
        process.StartInfo.ArgumentList.Add("--file");
        process.StartInfo.ArgumentList.Add(dockerFile);
        process.StartInfo.ArgumentList.Add(FileSystemHelper.GitRepositoryRoot);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

        ConcurrentQueue<string> output = new();
        process.OutputDataReceived += (_, args) => EnqueueLine(output, args.Data);
        process.ErrorDataReceived += (_, args) => EnqueueLine(output, args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Could not build the {image.Name} Docker image (exit code {process.ExitCode}):{Environment.NewLine}" +
                string.Join(Environment.NewLine, output.TakeLast(MaxErrorOutputLines)));
        }

        _logger.LogInformation("Docker image {ImageName} built in {Elapsed}", image.Name, stopwatch.Elapsed);
    }

    private static void EnqueueLine(ConcurrentQueue<string> output, string? line)
    {
        if (line != null)
            output.Enqueue(line);
    }
}
