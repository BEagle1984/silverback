// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public interface IEndpoint
    {
        string Name { get; }

        IMessageSerializer Serializer { get; }

        BatchSettings Batch { get; }

        void Validate();
    }
}