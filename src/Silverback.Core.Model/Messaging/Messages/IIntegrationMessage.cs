// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A message that is sent over the message broker. It is further specialized as
///     <see cref="IIntegrationEvent" /> and <see cref="IIntegrationCommand" />.
/// </summary>
[SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
public interface IIntegrationMessage : IMessage
{
}
