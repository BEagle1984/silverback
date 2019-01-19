// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestEventThree : IIntegrationEvent
    {
        public string Content { get; set; }
        public Guid Id { get; set; }
    }
}