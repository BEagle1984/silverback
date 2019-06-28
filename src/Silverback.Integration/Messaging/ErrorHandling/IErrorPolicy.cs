// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// An error policy is used to handle errors that may occur while processing the incoming messages.
    /// </summary>
    public interface IErrorPolicy
    {
        bool CanHandle(IEnumerable<IRawInboundMessage> messages, Exception exception);

        ErrorAction HandleError(IEnumerable<IRawInboundMessage> messages, Exception exception);
    }
}