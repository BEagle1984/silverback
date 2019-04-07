// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.Rx.TestTypes.Messages.Base
{
    public interface IRequest<out TResponse> : IMessage
    {
    }
}