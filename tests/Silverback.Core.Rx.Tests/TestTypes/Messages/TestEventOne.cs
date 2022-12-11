// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Core.Rx.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.Rx.TestTypes.Messages;

public class TestEventOne : IEvent, ITestMessage
{
    public string? Message { get; set; }
}
