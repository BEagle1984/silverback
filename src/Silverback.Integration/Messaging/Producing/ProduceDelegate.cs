// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Used by the <see cref="DelegatedProducer" />.
/// </summary>
internal delegate Task ProduceDelegate(IOutboundEnvelope envelope);
