// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Silverback.Tests
{
    public class FakeHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _startedCancellationTokenSource = new();

        private readonly CancellationTokenSource _stoppingCancellationTokenSource = new();

        private readonly CancellationTokenSource _stoppedCancellationTokenSource = new();

        public CancellationToken ApplicationStarted => _startedCancellationTokenSource.Token;

        public CancellationToken ApplicationStopping => _stoppingCancellationTokenSource.Token;

        public CancellationToken ApplicationStopped => _stoppedCancellationTokenSource.Token;

        public void TriggerApplicationStarted() => _startedCancellationTokenSource.Cancel();

        public void TriggerApplicationStopping() => _stoppingCancellationTokenSource.Cancel();

        public void TriggerApplicationStopped() => _stoppedCancellationTokenSource.Cancel();

        public void StopApplication() => throw new NotSupportedException();
    }
}
