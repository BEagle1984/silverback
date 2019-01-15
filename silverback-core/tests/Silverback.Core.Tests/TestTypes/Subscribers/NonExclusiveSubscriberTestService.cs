// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
{
    public class NonExclusiveSubscriberTestService : ISubscriber
    {
        public ConcurrentBag<DateTime> Timestamps { get; } = new ConcurrentBag<DateTime>();

        [Subscribe(Exclusive = false)]
        private void OnMessageReceived1(object _)
        {
            Thread.Sleep(20);

            Timestamps.Add(DateTime.Now);
        }

        [Subscribe(Exclusive = false)]
        private async Task OnMessageReceived2(object _)
        {
            await Task.Delay(20);

            Timestamps.Add(DateTime.Now);
        }
    }
}