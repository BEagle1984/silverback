// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

internal static class DefaultDeserializers
{
    public static IMessageDeserializer Json { get; } = new JsonMessageDeserializer<object>();

    public static IMessageDeserializer Binary { get; } = new BinaryMessageDeserializer<BinaryMessage>();
}
