// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.PerformanceTester.Messages;
using Silverback.PerformanceTester.Subscribers;

namespace Silverback.PerformanceTester.Testers
{
    internal class DirectMethodCallTester : TesterBase
    {
        public DirectMethodCallTester(int iterations) : base(iterations)
        {
        }

        protected override void PerformIteration()
        {
            ServiceProvider.GetRequiredService<Subscriber>().OnEventReceived(new TestEvent());
        }

        protected override int GetReceivedMessagesCount() =>
            ServiceProvider.GetRequiredService<Subscriber>().ReceivedMessagesCount;
    }
}