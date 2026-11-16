// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="OutboxSettings" />.
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "The builder is a public extension point that the storage packages extend with their own settings through extension methods")]
public class OutboxSettingsBuilder;
