// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.PerformanceTester.Messages;
using Silverback.PerformanceTester.Subscribers;

namespace Silverback.PerformanceTester.Testers
{
    internal class InternalQueryTester : TesterBase
    {
        public InternalQueryTester(int iterations) : base(iterations)
        {
        }

        protected override void PerformIteration()
        {
            var result = ServiceProvider.GetRequiredService<IQueryPublisher>().Execute(new TestQuery());

            if (result.Count() != 1 || result.FirstOrDefault() == null) throw new Exception("No result.");
        }

        protected override int GetReceivedMessagesCount() =>
            ServiceProvider.GetRequiredService<Subscriber>().ReceivedMessagesCount;
    }
}