// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types.Domain
{
    public class TestEventWithHeaders : IIntegrationEvent, ITestEventWithHeaders
    {
        [Header("x-readonly-string")]
        public string ReadOnlyStringHeader { get; } = "readonly";

        [Header("x-readonly-int")]
        public int ReadOnlyIntHeader { get; } = 42;

        public string? Content { get; set; }

        [Header("x-string")]
        public string? StringHeader { get; set; }

        [Header("x-string-default", PublishDefaultValue = true)]
        public string? StringHeaderWithDefault { get; set; }

        [Header("x-int")]
        public int IntHeader { get; set; }

        [Header("x-int-default", PublishDefaultValue = true)]
        public int IntHeaderWithDefault { get; set; }

        public string? InheritedHeader { get; set; }
    }
}
