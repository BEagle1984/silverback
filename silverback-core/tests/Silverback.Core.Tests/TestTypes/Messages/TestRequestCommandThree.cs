// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Core.Tests.TestTypes.Messages.Base;

namespace Silverback.Core.Tests.TestTypes.Messages
{
    public class TestRequestCommandThree : IRequest<IEnumerable<string>>, ITestMessage
    {
        public string Message { get; set; }
    }
}