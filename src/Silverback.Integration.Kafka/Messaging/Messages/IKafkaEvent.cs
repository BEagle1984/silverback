// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     An event that is triggered by the <see cref="Confluent.Kafka.Producer{TKey,TValue}" /> or
    ///     <see cref="Confluent.Kafka.Consumer{TKey,TValue}" /> from the underlying Confluent.Kafka library.
    /// </summary>
    [SuppressMessage("", "CA1040", Justification = Justifications.BaseInterface)]
    public interface IKafkaEvent : ISilverbackEvent
    {
    }
}
