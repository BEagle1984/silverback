// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class BrokerClientCollectionFixture
{
    [Fact]
    public void Add_ShouldAddClient()
    {
        BrokerClientCollection brokerClientCollection = [];
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        IBrokerClient client2 = Substitute.For<IBrokerClient>();

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        brokerClientCollection.Should().HaveCount(2);
        brokerClientCollection.Should().BeEquivalentTo(new[] { client1, client2 });
    }

    [Fact]
    public async Task ConnectAllAsync_ShouldConnectAllClients()
    {
        BrokerClientCollection brokerClientCollection = [];
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        IBrokerClient client2 = Substitute.For<IBrokerClient>();

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        await brokerClientCollection.ConnectAllAsync();

        await client1.Received(1).ConnectAsync();
        await client2.Received(1).ConnectAsync();
    }

    [Fact]
    public async Task DisconnectAllAsync_ShouldConnectAllClients()
    {
        BrokerClientCollection brokerClientCollection = [];

        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        IBrokerClient client2 = Substitute.For<IBrokerClient>();

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        await brokerClientCollection.DisconnectAllAsync();

        await client1.Received(1).DisconnectAsync();
        await client2.Received(1).DisconnectAsync();
    }
}
