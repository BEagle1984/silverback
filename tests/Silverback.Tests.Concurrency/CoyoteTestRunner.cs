// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.SystematicTesting;
using Xunit.Abstractions;
using Xunit.Sdk;
using CoyoteConfiguration = Microsoft.Coyote.Configuration;

namespace Silverback.Tests.Concurrency;

// Shared helper that runs an async test body under the Microsoft.Coyote systematic testing
// engine. Each test class takes an ITestOutputHelper and forwards it here so Coyote's report
// and any bug traces land in the xunit test output.
//
// The test assemblies (Silverback.Core, Silverback.Integration, and this test DLL) are
// binary-rewritten by `coyote rewrite` as a post-build step in the .csproj so that async
// state machines, SemaphoreSlim, Task.Run, etc. are intercepted by Coyote's scheduler.
internal static class CoyoteTestRunner
{
    // Default iteration count for most tests. Raise to 10k+ (and pass explicitly) for races
    // that require aggressive scheduling exploration to reproduce.
    public const int DefaultIterations = 100;

    public static void Run(Func<Task> testBody, ITestOutputHelper output, int iterations = DefaultIterations)
    {
        CoyoteConfiguration config = CoyoteConfiguration.Create()
            .WithTestingIterations((uint)iterations)
            .WithVerbosityEnabled()
            .WithConsoleLoggingEnabled();

        TestingEngine engine = TestingEngine.Create(config, testBody);
        engine.Run();

        string report = engine.GetReport();
        output.WriteLine(report);

        if (engine.TestReport.NumOfFoundBugs > 0)
        {
            string bugReports = string.Join("\n---\n", engine.TestReport.BugReports);
            output.WriteLine("Bug reports:");
            output.WriteLine(bugReports);
            throw new XunitException(
                $"Coyote found {engine.TestReport.NumOfFoundBugs} bug(s) across {iterations} iterations.\n\n{report}\n\nBug reports:\n{bugReports}");
        }
    }
}
