// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging
{
    public abstract class KafkaEndpoint : Endpoint
    {
        protected KafkaEndpoint(string name) : base(name)
        {
        }
    }
}
