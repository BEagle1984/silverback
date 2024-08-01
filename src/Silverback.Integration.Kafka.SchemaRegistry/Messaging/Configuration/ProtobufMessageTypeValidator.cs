// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Google.Protobuf;

namespace Silverback.Messaging.Configuration;

internal static class ProtobufMessageTypeValidator
{
    public static void Validate(Type messageType)
    {
        if (!typeof(IMessage).IsAssignableFrom(messageType))
            throw new SilverbackConfigurationException($"{messageType.Name} does not implement IMessage<{messageType.Name}>.");

        if (messageType.GetInterfaces()
            .All(
                interfaceType => !interfaceType.IsGenericType ||
                                 interfaceType.GetGenericTypeDefinition() != typeof(IMessage<>) ||
                                 interfaceType.GetGenericArguments().First() != messageType))
        {
            throw new SilverbackConfigurationException($"{messageType.Name} does not implement IMessage<{messageType.Name}>.");
        }

        if (messageType.GetConstructor(Type.EmptyTypes) == null)
            throw new SilverbackConfigurationException($"{messageType.Name} does not have a public parameterless constructor.");
    }
}
