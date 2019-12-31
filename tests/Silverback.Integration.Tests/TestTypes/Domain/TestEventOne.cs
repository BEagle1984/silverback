// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    public class TestEventOne : IIntegrationEvent
    {
        public string Content { get; set; }
        public Guid Id { get; set; }
    }
}