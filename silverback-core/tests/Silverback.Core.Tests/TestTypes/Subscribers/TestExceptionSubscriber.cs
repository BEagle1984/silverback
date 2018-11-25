// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestExceptionSubscriber : ISubscriber
    {
        [Subscribe]
        void OnMessageReceived(TestEventOne message) => throw new Exception("Test");

        [Subscribe]
        Task OnMessageReceivedAsync(TestEventTwo message) => throw new Exception("Test");
    }
}
