// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Core.Rx.Tests.TestTypes.Messages.Base;

namespace Silverback.Core.Rx.Tests.TestTypes.Messages
{
    public class TestCommandTwo : ICommand, ITestMessage
    {
        public string Message { get; set; }
    }
}