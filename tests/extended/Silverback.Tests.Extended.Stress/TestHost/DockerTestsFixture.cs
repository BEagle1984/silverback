// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Silverback.Tests.Extended.Stress.TestHost;

public abstract class DockerTestsFixture : IAsyncLifetime
{
    public const string WorkerImage = "silverback-stress-tests:local";

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    protected abstract IReadOnlyCollection<string> InfrastructureServices { get; }

    public static async Task<string> RunDockerAsync(TimeSpan timeout, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("docker")
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Docker.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource cancellation = new(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);

            throw new TimeoutException($"Docker command timed out: {string.Join(' ', arguments)}");
        }

        string result = await output;
        string diagnostics = await error;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Docker exited with {process.ExitCode}: {diagnostics}\n{result}");

        return result;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(RepositoryRoot, "tests/extended/artifacts"));

        // Reuse developer infrastructure. Do not tear down its containers, network, or volumes on disposal.
        await RunDockerAsync(
            TimeSpan.FromMinutes(3),
            [
                "compose",
                "--project-name", "silverback",
                "--file", Path.Combine(RepositoryRoot, "docker-compose.yaml"),
                "up",
                "--detach",
                "--no-recreate",
                .. InfrastructureServices
            ]);

        await WaitForInfrastructureAsync();

        await InitializeCoreAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected abstract Task WaitForInfrastructureAsync();

    protected virtual Task InitializeCoreAsync() => Task.CompletedTask;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Silverback.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Cannot find Silverback.sln above the test output directory.");
    }
}
