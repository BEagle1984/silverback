// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Transactions;

public class PublisherKafkaTransactionExtensionsFixture
{
    [Fact]
    public void InitKafkaTransaction_ShouldInitAndReturnTransaction()
    {
        SilverbackContext context = new();
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Context.Returns(context);

        IKafkaTransaction transaction = publisher.InitKafkaTransaction();

        transaction.ShouldNotBeNull();
        context.GetKafkaTransaction().Should().BeSameAs(transaction);
    }
}
