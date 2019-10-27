// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class LimitedParallelSubscriberTestService : ISubscriber
    {
        public ParallelTestingUtil Parallel { get; } = new ParallelTestingUtil();

        [Subscribe(Parallel = true, MaxDegreeOfParallelism = 2)]
        private void OnMessageReceived(object _) => Parallel.DoWork();

        [Subscribe(Parallel = true, MaxDegreeOfParallelism = 2)]
        private Task OnMessageReceivedAsync(object _) => Parallel.DoWorkAsync();
    }
}