// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Silverback.Messaging.Configuration.Mqtt;

internal class ParsedTopic
{
    private const string SharedSubscriptionPrefix = "$share/";

    public ParsedTopic(string topic)
    {
        if (TrySplitSharedSubscription(topic, out string? group, out string? actualTopic))
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

    // Splits '$share/{ShareName}/{TopicFilter}', returning an empty filter if the separator is missing
    public static bool TrySplitSharedSubscription(
        string topic,
        [NotNullWhen(true)] out string? group,
        [NotNullWhen(true)] out string? actualTopic)
    {
        if (!topic.StartsWith(SharedSubscriptionPrefix, StringComparison.Ordinal))
        {
            group = null;
            actualTopic = null;
            return false;
        }

        int separatorIndex = topic.IndexOf('/', SharedSubscriptionPrefix.Length);

        if (separatorIndex < 0)
        {
            group = topic[SharedSubscriptionPrefix.Length..];
            actualTopic = string.Empty;
            return true;
        }

        group = topic[SharedSubscriptionPrefix.Length..separatorIndex];
        actualTopic = topic[(separatorIndex + 1)..];
        return true;
    }

    public bool IsMatch(string topic) => Regex?.IsMatch(topic) ?? Topic == topic;

    // Builds the regex level by level, assuming a valid filter (each wildcard occupying an entire level and '#' being the last)
    [SuppressMessage("Security", "S6444:Not specifying a timeout for regular expressions is security-sensitive", Justification = "The levels are escaped and the wildcards can't cause catastrophic backtracking")]
    private static Regex? GetRegex(string topic)
    {
        if (!topic.Contains('+', StringComparison.Ordinal) && !topic.Contains('#', StringComparison.Ordinal))
            return null;

        string[] levels = topic.Split('/');
        StringBuilder pattern = new("^");

        // The topics starting with '$' (e.g. '$SYS/...') aren't matched by the filters starting with a wildcard
        if (levels[0] is "+" or "#")
            pattern.Append(@"(?!\$)");

        for (int i = 0; i < levels.Length; i++)
        {
            // The multi-level wildcard also matches the parent level ('a/#' matches 'a')
            if (levels[i] == "#")
            {
                pattern.Append(i == 0 ? ".*" : "(?:/.*)?");
                break;
            }

            if (i > 0)
                pattern.Append('/');

            pattern.Append(levels[i] == "+" ? "[^/]*" : Regex.Escape(levels[i]));
        }

        pattern.Append(@"\z");

        return new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);
    }
}
