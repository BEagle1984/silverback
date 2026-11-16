// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Lock;

namespace Silverback.Configuration;

/// <summary>
///     Builds the <see cref="DistributedLockSettings" />.
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "The builder is a public extension point that the lock implementations extend through extension methods")]
public class DistributedLockSettingsBuilder;
