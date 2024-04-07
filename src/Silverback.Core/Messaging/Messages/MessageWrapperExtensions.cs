// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

internal static class MessageWrapperExtensions
{
    public static Type GetMessageType(this IMessageWrapper messageWrapper)
    {
        if (messageWrapper.Message != null)
            return messageWrapper.Message.GetType();

        if (messageWrapper.GetType().IsGenericType)
            return messageWrapper.GetType().GetGenericArguments()[0];

        return typeof(object);
    }
}
