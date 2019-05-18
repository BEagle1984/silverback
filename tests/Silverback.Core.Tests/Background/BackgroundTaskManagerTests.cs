// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Background;
using Silverback.Tests.Core.TestTypes;
using Silverback.Tests.Core.TestTypes.Background;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public class BackgroundTaskManagerTests
    {
        [Fact]
        public void New_NoLock_TaskIsExecuted()
        {
            var manager = new BackgroundTaskManager(null, new NullLoggerFactory());
            bool executed = false;

            manager.Start("test", () => executed = true);

            AsyncTestingUtil.Wait(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        public void New_CancellationRequested_CancellationRequestReceived()
        {
            var manager = new BackgroundTaskManager(null, new NullLoggerFactory());
            var cancellationTokenSource = new CancellationTokenSource();
            bool canceled = false;

            manager.Start("test", () =>
                {
                    while (true)
                    {
                        canceled = cancellationTokenSource.Token.IsCancellationRequested;

                        if (canceled)
                            break;

                        Thread.Sleep(100);
                    }
                });

            cancellationTokenSource.Cancel();

            AsyncTestingUtil.Wait(() => canceled);

            canceled.Should().BeTrue();
        }

        [Fact]
        public void New_WithLock_OnlyOneTaskIsExecutedSimultaneously()
        {
            var manager = new BackgroundTaskManager(new TestLockManager(), new NullLoggerFactory());
            var cancellationTokenSource = new CancellationTokenSource();
            bool executed1 = false;
            bool executed2 = false;

            manager.Start("test", () =>
                {
                    executed1 = true;

                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(10);
                    }
                });

            AsyncTestingUtil.Wait(() => executed1);

            manager.Start("test", () => executed2 = true);

            AsyncTestingUtil.Wait(() => executed2, 100);

            executed1.Should().BeTrue();
            executed2.Should().BeFalse();

            cancellationTokenSource.Cancel();
            AsyncTestingUtil.Wait(() => executed2);

            executed2.Should().BeTrue();
        }

        [Fact]
        public void New_WithLock_HeartbeatIsSent()
        {
            var lockManager = new TestLockManager();
            var manager = new BackgroundTaskManager(lockManager, new NullLoggerFactory());
            var cancellationTokenSource = new CancellationTokenSource();

            manager.Start("test", () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(10);
                }
            });

            Thread.Sleep(200);
            cancellationTokenSource.Cancel();

            lockManager.Heartbeats.Should().BeGreaterOrEqualTo(3);
        }
    }
}