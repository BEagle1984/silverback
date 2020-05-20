// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    [SuppressMessage("", "UnusedMember.Local")]
    [SuppressMessage("", "UnusedParameter.Local")]
    public class TestExceptionSubscriber : ISubscriber
    {
        [Subscribe]
        void OnMessageReceived(TestEventOne message) => throw new Exception("Test");

        [Subscribe]
        Task OnMessageReceivedAsync(TestEventTwo message) => throw new Exception("Test");
    }
}