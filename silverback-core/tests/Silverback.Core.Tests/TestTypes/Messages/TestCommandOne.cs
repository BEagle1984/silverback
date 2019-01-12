// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Core.Tests.TestTypes.Messages
{
    public class TestCommandOne : ICommand, ITestMessage
    {
        public string Message { get; set; }
    }
}