// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Wraps the consumed bytes stream into an <see cref="IBinaryMessage" />.
/// </summary>
internal interface IBinaryMessageDeserializer : IMessageDeserializer;
