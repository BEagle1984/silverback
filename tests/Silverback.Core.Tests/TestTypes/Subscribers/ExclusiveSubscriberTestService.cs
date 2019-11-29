// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public class ExclusiveSubscriberTestService : ISubscriber
    {
        public ParallelTestingUtil Parallel { get; } = new ParallelTestingUtil();

        [Subscribe(Exclusive = true)]
        private void OnMessageReceived(object _) => Parallel.DoWork();

        [Subscribe(Exclusive = false)]
        private Task OnMessageReceivedAsync(object _) => Parallel.DoWorkAsync();
    }
}