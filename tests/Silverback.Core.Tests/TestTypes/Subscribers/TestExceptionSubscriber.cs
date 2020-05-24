// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestExceptionSubscriber : ISubscriber
    {
        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        private void OnMessageReceived(TestEventOne message) => throw new InvalidOperationException("Test");

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        private Task OnMessageReceivedAsync(TestEventTwo message) => throw new InvalidOperationException("Test");
    }
}
