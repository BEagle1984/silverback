// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class MultipleGroupsMessage : IIntegrationMessage
    {
        public string Content { get; set; }
    }
}