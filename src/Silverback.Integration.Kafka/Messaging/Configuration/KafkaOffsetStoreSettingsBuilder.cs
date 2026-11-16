// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="KafkaOffsetStoreSettings" />.
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "Public extension point on which the offset store settings are added by extension methods")]
public class KafkaOffsetStoreSettingsBuilder;
