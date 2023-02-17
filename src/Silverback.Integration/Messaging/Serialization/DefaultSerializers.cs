// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryMessages;

namespace Silverback.Messaging.Serialization;

internal static class DefaultSerializers
{
    public static IMessageSerializer Json { get; } = new JsonMessageSerializer();

    public static IMessageSerializer Binary { get; } = new BinaryMessageSerializer();
}
