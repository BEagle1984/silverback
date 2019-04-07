// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Messages
{
    public class TestEventOne : IEvent, ITestMessage
    {
        public string Message { get; set; }
    }
}
