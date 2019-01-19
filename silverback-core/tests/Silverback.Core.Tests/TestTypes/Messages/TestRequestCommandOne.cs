// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Core.Tests.TestTypes.Messages.Base;

namespace Silverback.Core.Tests.TestTypes.Messages
{
    public class TestRequestCommandOne : ICommand<string>, ITestMessage
    {
        public string Message { get; set; }
    }
}
