// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent a request message awaiting a response (see <see cref="IResponse"/>).
    /// </summary>
    public interface IRequest : IMessage
    {
        Guid RequestId { get; set; }
    }
}