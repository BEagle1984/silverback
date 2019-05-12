// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Background;
using Silverback.Tests.Core.TestTypes;
using Silverback.Tests.Core.TestTypes.Background;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public class BackgroundTaskTests
    {
        [Fact]
        public void New_NoLock_TaskIsExecuted()
        {
            bool executed = false;

            new BackgroundTask("test", () => executed = true, 
                null, null, Substitute.For<ILogger<BackgroundTask>>());

            AsyncTestingUtil.Wait(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        public void New_CancellationRequested_CancellationRequestReceived()
        {
            bool canceled = false;

            var cancellationTokenSource = new CancellationTokenSource();
            new BackgroundTask("test", () =>
                {
                    while (true)
                    {
                        canceled = cancellationTokenSource.Token.IsCancellationRequested;

                        if (canceled)
                            break;

                        Thread.Sleep(100);
                    }
                },
                null, null, Substitute.For<ILogger<BackgroundTask>>());

            cancellationTokenSource.Cancel();

            AsyncTestingUtil.Wait(() => canceled);

            canceled.Should().BeTrue();
        }

        [Fact]
        public void New_WithLock_OnlyOneTaskIsExecutedSimultaneously()
        {
            var lockManager = new TestLockManager();
            bool executed1 = false;
            bool executed2 = false;

            var cancellationTokenSource = new CancellationTokenSource();
            new BackgroundTask("test", () =>
                {
                    executed1 = true;

                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(10);
                    }
                },
                null, lockManager, Substitute.For<ILogger<BackgroundTask>>());

            AsyncTestingUtil.Wait(() => executed1);

            new BackgroundTask("test", () => executed2 = true,
                null, lockManager, Substitute.For<ILogger<BackgroundTask>>());

            AsyncTestingUtil.Wait(() => executed2, 100);

            executed1.Should().BeTrue();
            executed2.Should().BeFalse();

            cancellationTokenSource.Cancel();
            AsyncTestingUtil.Wait(() => executed2);

            executed2.Should().BeTrue();
        }
    }
}