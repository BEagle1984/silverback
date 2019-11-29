// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public interface IRequest<out TResponse> : IMessage
    {
    }
}