// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public class TestEventWithHeaders : IIntegrationEvent
{
    [JsonIgnore]
    [Header("x-custom-header")]
    public string? CustomHeader { get; set; }

    [JsonIgnore]
    [Header("x-custom-header2", PublishDefaultValue = true)]
    public bool CustomHeader2 { get; set; }

    public string? Content { get; set; }
}
