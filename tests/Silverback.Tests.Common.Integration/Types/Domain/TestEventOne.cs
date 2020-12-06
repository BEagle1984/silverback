// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Types.Domain
{
    public class TestEventOne : IIntegrationEvent
    {
        public string? Content { get; set; }
    }
}
