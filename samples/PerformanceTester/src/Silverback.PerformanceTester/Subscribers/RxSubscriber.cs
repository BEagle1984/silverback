// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Core.Messaging;
using Silverback.PerformanceTester.Messages;

namespace Silverback.PerformanceTester.Subscribers
{
    public class RxSubscriber : SubscriberBase, IDisposable
    {
        private readonly IDisposable _subscription;

        public RxSubscriber(IMessageObservable<TestRxEvent> observable)
        {
            _subscription = observable.Subscribe(HandleMessage);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}