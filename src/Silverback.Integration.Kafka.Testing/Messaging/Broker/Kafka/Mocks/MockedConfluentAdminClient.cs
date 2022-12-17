// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class MockedConfluentAdminClient : IAdminClient
{
    private readonly IMockedKafkaOptions _options;

    public MockedConfluentAdminClient(ClientConfig config, IMockedKafkaOptions options)
    {
        _options = options;
        Name = $"MockedAdminClient.{config.ClientId}.{Guid.NewGuid()}";
    }

    public string Name { get; }

    public Handle Handle { get; } = new();

    public int AddBrokers(string brokers) => throw new NotSupportedException();

    public List<GroupInfo> ListGroups(TimeSpan timeout) => throw new NotSupportedException();

    public GroupInfo ListGroup(string group, TimeSpan timeout) => throw new NotSupportedException();

    public Metadata GetMetadata(string topic, TimeSpan timeout)
    {
        List<PartitionMetadata> partitionsMetadata =
            Enumerable.Range(0, _options.DefaultPartitionsCount)
                .Select(
                    i => new PartitionMetadata(
                        i,
                        0,
                        new[] { 0 },
                        new[] { 0 },
                        null))
                .ToList();

        return new Metadata(
            new List<BrokerMetadata>
            {
                new(0, "test", 42)
            },
            new List<TopicMetadata>
            {
                new(topic, partitionsMetadata, null)
            },
            0,
            "test");
    }

    public Metadata GetMetadata(TimeSpan timeout) => throw new NotSupportedException();

    public Task CreatePartitionsAsync(
        IEnumerable<PartitionsSpecification> partitionsSpecifications,
        CreatePartitionsOptions? options = null) => throw new NotSupportedException();

    public Task DeleteGroupsAsync(IList<string> groups, DeleteGroupsOptions? options = null) =>
        throw new NotSupportedException();

    public Task DeleteTopicsAsync(IEnumerable<string> topics, DeleteTopicsOptions? options = null) =>
        throw new NotSupportedException();

    public Task CreateTopicsAsync(
        IEnumerable<TopicSpecification> topics,
        CreateTopicsOptions? options = null) => throw new NotSupportedException();

    public Task AlterConfigsAsync(
        Dictionary<ConfigResource, List<ConfigEntry>> configs,
        AlterConfigsOptions? options = null) => throw new NotSupportedException();

    public Task<List<DescribeConfigsResult>> DescribeConfigsAsync(
        IEnumerable<ConfigResource> resources,
        DescribeConfigsOptions? options = null) => throw new NotSupportedException();

    public Task<List<DeleteRecordsResult>> DeleteRecordsAsync(
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets,
        DeleteRecordsOptions? options = null) => throw new NotSupportedException();

    public Task CreateAclsAsync(IEnumerable<AclBinding> aclBindings, CreateAclsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<DescribeAclsResult> DescribeAclsAsync(AclBindingFilter aclBindingFilter, DescribeAclsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<List<DeleteAclsResult>> DeleteAclsAsync(IEnumerable<AclBindingFilter> aclBindingFilters, DeleteAclsOptions? options = null) =>
        throw new NotSupportedException();

    public void Dispose()
    {
        // Nothing to dispose, it's just a mock
    }
}
