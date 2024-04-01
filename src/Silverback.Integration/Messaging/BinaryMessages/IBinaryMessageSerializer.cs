// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Handles the <see cref="IBinaryMessage" />. It's not really a serializer, since the raw binary content is transmitted as-is.
/// </summary>
internal interface IBinaryMessageSerializer : IMessageSerializer;
