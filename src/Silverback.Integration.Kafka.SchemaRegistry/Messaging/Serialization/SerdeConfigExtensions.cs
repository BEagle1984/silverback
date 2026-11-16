// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Serialization;

internal static class SerdeConfigExtensions
{
    // The subject name strategy must match on both sides (the deserializer uses it to look up the latest version) and the serializer has a
    // counterpart setting with the same name and meaning
    private const string SubjectNameStrategySetting = "subject.name.strategy";

    // The other deserializer-specific settings have no serializer counterpart or a different meaning, and are rejected (e.g. Avro) or
    // ignored by the serializers, while the settings without prefix (e.g. the rules) are shared
    public static IEnumerable<KeyValuePair<string, string>> GetSettingsSharedWithSerializer(
        this IEnumerable<KeyValuePair<string, string>> deserializerConfig,
        string format)
    {
        string deserializerSettingsPrefix = $"{format}.deserializer.";

        foreach (KeyValuePair<string, string> setting in deserializerConfig)
        {
            if (!setting.Key.StartsWith(deserializerSettingsPrefix, StringComparison.Ordinal))
                yield return setting;
            else if (setting.Key == deserializerSettingsPrefix + SubjectNameStrategySetting)
                yield return new KeyValuePair<string, string>($"{format}.serializer.{SubjectNameStrategySetting}", setting.Value);
        }
    }
}
