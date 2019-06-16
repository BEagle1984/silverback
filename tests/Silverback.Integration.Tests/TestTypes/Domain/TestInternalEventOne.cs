// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    public class TestInternalEventOne : IEvent
    {
        public string InternalMessage { get; set; }
    }
}