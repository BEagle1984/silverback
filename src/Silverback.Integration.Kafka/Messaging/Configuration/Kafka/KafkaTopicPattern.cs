// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

// Like librdkafka, treats the topics starting with '^' as regular expressions (a topic name cannot contain a '^'). The routing
// evaluates them with .NET, while librdkafka (classic protocol) or the broker (consumer protocol) select the subscribed topics.
// Like librdkafka, a match at the beginning of the name is enough with the classic protocol, while the broker (RE2/J) requires the
// pattern to match the entire name with the consumer protocol.
[SuppressMessage("Security", "S6444:Not specifying a timeout for regular expressions is security-sensitive", Justification = "The patterns come from the application configuration and are matched against the topic names only")]
internal static class KafkaTopicPattern
{
    public static bool IsPattern(string topic) => topic.StartsWith('^');

    public static bool IsFullMatchRequired(GroupProtocol? groupProtocol) => groupProtocol == GroupProtocol.Consumer;

    public static Regex Create(string pattern, bool fullMatch = false) =>
        new(GetExpression(pattern, fullMatch), RegexOptions.CultureInvariant);

    public static bool IsMatch(string subscription, string topic, bool fullMatch = false) =>
        IsPattern(subscription)
            ? Regex.IsMatch(topic, GetExpression(subscription, fullMatch), RegexOptions.CultureInvariant)
            : subscription == topic;

    // Anchored at both ends to emulate RE2/J's Matcher.matches(), also when the pattern contains unanchored alternatives
    private static string GetExpression(string pattern, bool fullMatch) => fullMatch ? $@"\A(?:{pattern})\z" : pattern;
}
