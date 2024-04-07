// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     The values of the properties decorated with this attribute are used to build the message key that
///     will be used by Kafka (for partitioning, compacting, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class KafkaKeyMemberAttribute : Attribute;
