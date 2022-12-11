// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.Kafka.TestTypes.Messages;

public class NoKeyMembersMessage : IMessage
{
    public Guid Id { get; set; }

    public string? One { get; set; }

    public string? Two { get; set; }

    public string? Three { get; set; }
}
