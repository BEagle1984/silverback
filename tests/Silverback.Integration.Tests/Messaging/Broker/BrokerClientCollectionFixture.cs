// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
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
        client1.Name.Returns("client1");
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        brokerClientCollection.Count.ShouldBe(2);
        brokerClientCollection.ShouldBe([client1, client2]);
    }

    [Fact]
    public void Add_ShouldThrow_WhenNameNotUnique()
    {
        BrokerClientCollection brokerClientCollection = [];
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client1");

        brokerClientCollection.Add(client1);

        Action act = () => brokerClientCollection.Add(client2);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("A client with name 'client1' has already been added.");
    }

    [Fact]
    public void Indexer_ShouldGetClientByName()
    {
        BrokerClientCollection brokerClientCollection = [];
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        brokerClientCollection["client1"].ShouldBe(client1);
        brokerClientCollection["client2"].ShouldBe(client2);
    }

    [Fact]
    public async Task ConnectAllAsync_ShouldConnectAllClients()
    {
        BrokerClientCollection brokerClientCollection = [];
        IBrokerClient client1 = Substitute.For<IBrokerClient>();
        client1.Name.Returns("client1");
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");

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
        client1.Name.Returns("client1");
        IBrokerClient client2 = Substitute.For<IBrokerClient>();
        client2.Name.Returns("client2");

        brokerClientCollection.Add(client1);
        brokerClientCollection.Add(client2);

        await brokerClientCollection.DisconnectAllAsync();

        await client1.Received(1).DisconnectAsync();
        await client2.Received(1).DisconnectAsync();
    }
}
