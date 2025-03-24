// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.Rx.TestTypes.Messages;

public class TestEnvelope : IEnvelope
{
    public TestEnvelope(object message)
    {
        Message = message;
    }

    public object? Message { get; }

    public Type MessageType => Message?.GetType() ?? typeof(object);
}
