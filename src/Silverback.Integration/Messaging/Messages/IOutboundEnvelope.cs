// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawOutboundEnvelope" />
public interface IOutboundEnvelope : IBrokerEnvelope, IRawOutboundEnvelope
{
}
