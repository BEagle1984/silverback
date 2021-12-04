// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.Rx.TestTypes.Messages;

public class TestEnvelope : IEnvelope
{
    public TestEnvelope(object message, bool autoUnwrap = true)
    {
        Message = message;
        AutoUnwrap = autoUnwrap;
    }

    public bool AutoUnwrap { get; }

    public object? Message { get; set; }
}
