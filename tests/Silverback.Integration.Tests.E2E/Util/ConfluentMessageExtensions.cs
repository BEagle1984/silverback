// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Tests.Integration.E2E.Util;

public static class ConfluentMessageExtensions
{
    public static string? GetContentAsString(this Message<byte[]?, byte[]?> message) =>
        message.Value == null ? null : Encoding.UTF8.GetString(message.Value);

    public static IReadOnlyCollection<string?> GetContentAsString(this IEnumerable<Message<byte[]?, byte[]?>> messages) =>
        messages.Select(GetContentAsString).ToList();

    public static string GetValueAsString(this IHeader header) =>
        Encoding.UTF8.GetString(header.GetValueBytes());
}
