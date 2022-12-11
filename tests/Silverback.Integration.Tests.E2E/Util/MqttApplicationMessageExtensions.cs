// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet;

namespace Silverback.Tests.Integration.E2E.Util;

public static class MqttApplicationMessageExtensions
{
    public static string? GetContentAsString(this MqttApplicationMessage message) =>
        message.Payload == null ? null : Encoding.UTF8.GetString(message.Payload);

    public static IReadOnlyCollection<string?> GetContentAsString(this IEnumerable<MqttApplicationMessage> messages) =>
        messages.Select(GetContentAsString).ToList();
}
