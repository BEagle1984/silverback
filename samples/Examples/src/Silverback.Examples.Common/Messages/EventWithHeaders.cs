// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class EventWithHeaders : IntegrationEvent
    {
        [Header("x-string-header")]
        [JsonIgnore]
        public string StringHeader { get; set; }

        [Header("x-bool-header", PublishDefaultValue = true)]
        [JsonIgnore]
        public bool BoolHeader { get; set; }

        [Header("x-int-header")]
        [JsonIgnore]
        public int? IntHeader { get; set; }

        [Header("x-bool-header-2", PublishDefaultValue = true)]
        [JsonIgnore]
        public bool? BoolHeader2 { get; set; }
    }
}