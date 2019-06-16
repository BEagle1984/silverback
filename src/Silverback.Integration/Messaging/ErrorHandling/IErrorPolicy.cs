// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy is used to handle errors that may occur while processing the incoming messages.
    /// </summary>
    public interface IErrorPolicy
    {
        bool CanHandle(IInboundMessage message, Exception exception);

        ErrorAction HandleError(IInboundMessage message, Exception exception);
    }
}