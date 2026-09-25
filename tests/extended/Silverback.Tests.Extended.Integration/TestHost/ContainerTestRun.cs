// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services;
using Shouldly;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.TestHost;

public sealed class ContainerTestRun : IAsyncDisposable
{
    private readonly List<IContainerService> _containers = [];

    private readonly ITestOutputHelper _output;

    public ContainerTestRun(ITestOutputHelper output)
    {
        _output = output;
        Prefix = $"stress-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        Artifacts = Path.Combine(DockerTestsFixture.RepositoryRoot, "tests/extended/artifacts", Prefix);
        Directory.CreateDirectory(Artifacts);
        _output.WriteLine($"Artifacts: {Artifacts}");
    }

    public string Prefix { get; }

    public string Artifacts { get; }

    public static async Task<string> LogsAsync(IContainerService container) =>
        await DockerTestsFixture.RunDockerAsync(TimeSpan.FromSeconds(20), "logs", "--timestamps", container.Name);

    public async Task<IContainerService> StartAsync(string member, params string[] environment)
    {
        string name = $"{Prefix}-{member}";
        IContainerService container = new Builder()
            .UseContainer()
            .UseImage(DockerTestsFixture.WorkerImage)
            .WithName(name)
            .UseNetwork("silverback_default")
            .Mount(Artifacts, "/evidence", MountType.ReadWrite)
            .UseCapability("SYS_PTRACE")
            .WithEnvironment(["PREFIX=" + Prefix, "MEMBER=" + member, .. environment])
            .Build();

        _containers.Add(container);
        await File.WriteAllTextAsync(Path.Combine(Artifacts, $"{member}-settings.json"), JsonSerializer.Serialize(environment));
        await Task.Run(() => container.Start());
        _output.WriteLine($"Started {name}");

        return container;
    }

    public async Task StopAsync(IContainerService container, bool requireSuccess = true)
    {
        await Task.Run(container.Stop).WaitAsync(TimeSpan.FromSeconds(40));

        if (requireSuccess)
            container.GetConfiguration(true).State.ExitCode.ShouldBe(0, $"{container.Name} should shut down gracefully");

        _output.WriteLine($"Stopped {container.Name}");
    }

    public async Task WaitForLogAsync(IContainerService container, string marker, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            container.GetConfiguration(true).State.Running.ShouldBeTrue($"{container.Name} exited before '{marker}'");

            if ((await LogsAsync(container)).Contains(marker, StringComparison.Ordinal))
                return;

            await Task.Delay(200);
        }

        throw new TimeoutException($"{container.Name} did not report '{marker}'. Artifacts: {Artifacts}. {await LogsAsync(container)}");
    }

    public async Task CaptureDiagnosticsAsync(IContainerService container)
    {
        if (!container.GetConfiguration(true).State.Running)
            return;

        try
        {
            string stacks = await DockerTestsFixture.RunDockerAsync(
                TimeSpan.FromSeconds(20), "exec", container.Name, "/tools/dotnet-stack", "report", "--process-id", "1");

            await File.WriteAllTextAsync(Path.Combine(Artifacts, container.Name + "-stacks.txt"), stacks);
            await DockerTestsFixture.RunDockerAsync(
                TimeSpan.FromSeconds(40),
                [
                    "exec", container.Name,
                    "/tools/dotnet-dump", "collect",
                    "--process-id", "1",
                    "--type", "Mini",
                    "--output", "/evidence/" + container.Name + ".dmp"
                ]);
        }
        catch (Exception exception)
        {
            await File.WriteAllTextAsync(Path.Combine(Artifacts, container.Name + "-diagnostics-error.txt"), exception.ToString());
            _output.WriteLine($"Diagnostic capture failed: {exception.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<Exception> errors = [];

        foreach (IContainerService container in _containers.AsEnumerable().Reverse())
        {
            try
            {
                if (container.GetConfiguration(true).State.Running)
                {
                    // A container still running during disposal usually means the assertion failed before normal shutdown
                    await CaptureDiagnosticsAsync(container);
                    await StopAsync(container, false);
                }

                await File.WriteAllTextAsync(Path.Combine(Artifacts, $"{container.Name}.log"), await LogsAsync(container));
                await File.WriteAllTextAsync(
                    Path.Combine(Artifacts, $"{container.Name}-inspect.json"),
                    await DockerTestsFixture.RunDockerAsync(TimeSpan.FromSeconds(20), "inspect", container.Name));
            }
            catch (Exception exception)
            {
                errors.Add(exception);
                _output.WriteLine($"Artifact capture failed: {exception}");
            }
            finally
            {
                try
                {
                    container.Dispose();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }
        }

        if (errors.Count > 0)
            throw new AggregateException("Stress container cleanup failed.", errors);
    }
}
