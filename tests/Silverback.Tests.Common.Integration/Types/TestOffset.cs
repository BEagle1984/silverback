// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Types;

public sealed record TestOffset : IBrokerMessageIdentifier
{
    public TestOffset()
    {
        Key = "test";
        Value = Guid.NewGuid().ToString();
    }

    public TestOffset(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public string Value { get; }

    public string ToLogString() => Value;

    public string ToVerboseLogString() => $"{Key}@{Value}";

    public bool Equals(IBrokerMessageIdentifier? other) => other is TestOffset otherOffset && Equals(otherOffset);
}
