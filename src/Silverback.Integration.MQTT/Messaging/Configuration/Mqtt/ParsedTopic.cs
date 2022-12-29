// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Silverback.Messaging.Configuration.Mqtt;

// TODO: Test
internal class ParsedTopic
{
    public ParsedTopic(string topic)
    {
        if (IsSharedSubscription(topic, out string? group, out string? actualTopic))
        {
            topic = actualTopic;
            SharedSubscriptionGroup = group;
        }

        Topic = topic;
        Regex = GetRegex(topic);
    }

    public Regex? Regex { get; }

    public string Topic { get; }

    public string? SharedSubscriptionGroup { get; }

    private static bool IsSharedSubscription(
        string topic,
        [NotNullWhen(true)] out string? group,
        [NotNullWhen(true)] out string? actualTopic)
    {
        const string sharedSubscriptionPrefix = "$share/";
        if (topic.StartsWith(sharedSubscriptionPrefix, StringComparison.Ordinal))
        {
            group = topic[sharedSubscriptionPrefix.Length..topic.IndexOf('/', sharedSubscriptionPrefix.Length)];
            actualTopic = topic[(topic.IndexOf('/', sharedSubscriptionPrefix.Length) + 1)..];
            return true;
        }

        group = null;
        actualTopic = null;
        return false;
    }

    private static Regex? GetRegex(string topic)
    {
        if (!topic.Contains('+', StringComparison.Ordinal) && !topic.Contains('#', StringComparison.Ordinal))
        {
            return null;
        }

        string pattern = Regex.Escape(topic)
            .Replace("\\+", "[^\\/]*", StringComparison.Ordinal)
            .Replace("\\#", ".*", StringComparison.Ordinal);

        return new Regex($"^{pattern}$", RegexOptions.Compiled);
    }
}
