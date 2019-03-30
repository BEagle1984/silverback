using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class ParallelTestingUtil
    {
        private int _lastStep = 0;
        public ConcurrentBag<int> Steps { get; } = new ConcurrentBag<int>();

        public void DoWork()
        {
            Steps.Add(_lastStep + 1);
            Thread.Sleep(20);
            Interlocked.Increment(ref _lastStep);
        }

        public async Task DoWorkAsync()
        {
            Steps.Add(_lastStep + 1);
            await Task.Delay(20);
            Interlocked.Increment(ref _lastStep);
        }
    }
}
