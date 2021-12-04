// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.TestTypes;

public class SomeUnhandledMessageSubscriber
{
    public IList<SomeUnhandledMessage> ReceivedMessages { get; } = new List<SomeUnhandledMessage>();

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
    private void OnMessageReceived(SomeUnhandledMessage message) => ReceivedMessages.Add(message);
}
