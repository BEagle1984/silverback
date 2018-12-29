// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.PerformanceTester.Messages;
using Silverback.PerformanceTester.Subscribers;

namespace Silverback.PerformanceTester.Testers
{
    internal class InternalEventRxTester : TesterBase
    {
        public InternalEventRxTester(int iterations) : base(iterations)
        {
        }

        protected override void Setup()
        {
            ServiceProvider.GetRequiredService<RxSubscriber>();
        }

        protected override void PerformIteration()
        {
            ServiceProvider.GetRequiredService<IEventPublisher>().Publish(new TestRxEvent());
        }

        protected override int GetReceivedMessagesCount() =>
            ServiceProvider.GetRequiredService<RxSubscriber>().ReceivedMessagesCount;
    }
}