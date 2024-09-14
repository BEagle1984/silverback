// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.IO;
using Ductus.FluentDocker.Builders;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Configuration.Models;
using Silverback.TestBench.UI;
using Silverback.TestBench.Utils;

namespace Silverback.TestBench.Containers;

public static class DockerImagesBuilder
{
    public static void BuildAll()
    {
        foreach (DockerContainer dockerContainer in DockerContainers.GetAll())
        {
            Build(dockerContainer.ProjectName, dockerContainer.ImageName);
        }

        Console.WriteLine();
    }

    public static void Build(string projectName, string imageName)
    {
        string dockerFile = Path.Combine(FileSystemHelper.SolutionRoot, projectName, "Dockerfile");

        Console.Write($"Building docker image {imageName} from {dockerFile}...");
        Stopwatch stopwatch = Stopwatch.StartNew();

        new Builder()
            .DefineImage(imageName)
            .FromFile(dockerFile)
            .WorkingFolder(Path.Combine(Path.GetDirectoryName(dockerFile)!, "../.."))
            .Build().Start();

        ConsoleHelper.WriteDone(stopwatch.Elapsed);
    }
}
