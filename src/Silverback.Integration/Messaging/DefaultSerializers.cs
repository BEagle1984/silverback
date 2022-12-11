// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging;

internal static class DefaultSerializers
{
    public static IMessageSerializer Json { get; } = new JsonMessageSerializer<object>();

    public static IMessageSerializer Binary { get; } = new BinaryMessageSerializer<BinaryMessage>();
}
