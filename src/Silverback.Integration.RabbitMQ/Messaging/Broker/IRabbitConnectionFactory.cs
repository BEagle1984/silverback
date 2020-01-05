// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using RabbitMQ.Client;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Broker
{
    public interface IRabbitConnectionFactory : IDisposable
    {
        IModel GetChannel(RabbitProducerEndpoint endpoint);
        (IModel channel, string queueName) GetChannel(RabbitConsumerEndpoint endpoint);
        IConnection GetConnection(RabbitConnectionConfig connectionConfig);
    }
}