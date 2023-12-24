// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     A mocked implementation of <see cref="IProducer{TKey,TValue}" /> from Confluent.Kafka that produces to an <see cref="IInMemoryTopic" />.
/// </summary>
public interface IMockedConfluentProducer : IProducer<byte[]?, byte[]?>
{
}
