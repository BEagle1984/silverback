// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the deserialized inbound or outbound message.
/// </summary>
public interface IBrokerEnvelope : IRawBrokerEnvelope, IEnvelope;
