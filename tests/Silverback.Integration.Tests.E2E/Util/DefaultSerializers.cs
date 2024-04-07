// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.E2E.Util;

internal static class DefaultSerializers
{
    public static IMessageSerializer Json { get; } = new JsonMessageSerializer();

    public static IMessageSerializer Binary { get; } = new BinaryMessageSerializer();
}
