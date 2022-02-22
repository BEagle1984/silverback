// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound;

/// <summary>
///     Used by the <see cref="DelegatedProducer"/>.
/// </summary>
internal delegate Task ProduceDelegate(object? message, byte[]? messageBytes, IReadOnlyCollection<MessageHeader>? headers, ProducerEndpoint endpoint);
