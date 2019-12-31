// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    public interface IIntegrationMessage : IMessage
    {
        Guid Id { get; set; }
    }
}