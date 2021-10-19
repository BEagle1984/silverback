// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging;

namespace Silverback.Tests.Integration.TestTypes;

public sealed record TestOtherConsumerConfiguration : ConsumerConfiguration
{
    public TestOtherConsumerConfiguration(params string[] topicNames)
    {
        TopicNames = topicNames;
        RawName = string.Join(",", topicNames);
    }

    public IReadOnlyCollection<string> TopicNames { get; }

    public string GroupId { get; init; } = "default-group";

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public static TestOtherConsumerConfiguration GetDefault() => new("test");

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public TestActualOtherConsumerEndpoint GetEndpoint() => new("test", this);

    public override string GetUniqueConsumerGroupName() => $"{RawName}|{GroupId}";
}
