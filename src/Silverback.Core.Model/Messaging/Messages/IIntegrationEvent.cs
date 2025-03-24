// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A message that is sent over the message broker to notify an event.
/// </summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interface")]
public interface IIntegrationEvent : IEvent, IIntegrationMessage;
