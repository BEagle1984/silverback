// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Collections;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Types;

public sealed record TestConsumerEndpointConfiguration : ConsumerEndpointConfiguration
{
    private readonly IValueReadOnlyCollection<string> _topicNames = ValueReadOnlyCollection.Empty<string>();

    public TestConsumerEndpointConfiguration(params string[] topicNames)
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

    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "A new instance is desired")]
    public static TestConsumerEndpointConfiguration GetDefault() => new("test");

    public TestConsumerEndpoint GetDefaultEndpoint() => new(TopicNames.First(), this);

    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (TopicNames == null || TopicNames.Count == 0)
            throw new BrokerConfigurationException("At least 1 topic name is required.");

        if (TopicNames.Any(string.IsNullOrWhiteSpace))
            throw new BrokerConfigurationException("The topic name cannot be null or empty.");
    }
}
