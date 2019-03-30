// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.TestTypes.Messages
{
    public interface ITestMessage : IMessage
    {
        string Message { get; }
    }
}