// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Marks the consumers whose commit implicitly commits all the previous messages as well (e.g. the Kafka offsets of a partition), as
///     opposed to the consumers acknowledging each message individually (e.g. MQTT). Their commit must be deferred while a previous
///     sequence isn't resolved yet, not to commit its messages before they are processed.
/// </summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface checked by the consumer transaction")]
internal interface ICumulativeCommitConsumer;
