// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Collections;
using Silverback.Messaging;

namespace Silverback.Tests.Types;

public sealed record TestConsumerConfiguration : ConsumerConfiguration
{
    private readonly IValueReadOnlyCollection<string> _topicNames = ValueReadOnlyCollection.Empty<string>();

    public TestConsumerConfiguration(params string[] topicNames)
    {
        TopicNames = topicNames.AsValueReadOnlyCollection();
    }

    public IValueReadOnlyCollection<string> TopicNames
    {
        get => _topicNames;
        init
        {
            _topicNames = value;

            if (value != null)
                RawName = string.Join(",", value);
        }
    }

    public string GroupId { get; init; } = "default-group";

    [SuppressMessage("", "CA1024", Justification = "Method is appropriate (new instance)")]
    public static TestConsumerConfiguration GetDefault() => new("test");

    public TestConsumerEndpoint GetDefaultEndpoint() => new(TopicNames.First(), this);

    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (TopicNames == null || TopicNames.Count == 0)
            throw new EndpointConfigurationException($"At least 1 topic name is required.");

        if (TopicNames.Any(string.IsNullOrWhiteSpace))
            throw new EndpointConfigurationException($"The topic name cannot be null or empty.");
    }
}
