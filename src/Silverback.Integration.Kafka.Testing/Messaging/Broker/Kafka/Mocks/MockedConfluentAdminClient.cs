// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class MockedConfluentAdminClient : IAdminClient
{
    private static readonly Error NoError = new(ErrorCode.NoError);

    private readonly ClientConfig _config;

    private readonly IInMemoryTopicCollection _topics;

    public MockedConfluentAdminClient(ClientConfig config, IInMemoryTopicCollection topics)
    {
        _config = config;
        _topics = topics;
        Name = $"MockedAdminClient.{config.ClientId}.{Guid.NewGuid()}";
    }

    public string Name { get; }

    public Handle Handle { get; } = new();

    public int AddBrokers(string brokers) => throw new NotSupportedException();

    public List<GroupInfo> ListGroups(TimeSpan timeout) => throw new NotSupportedException();

    public GroupInfo ListGroup(string group, TimeSpan timeout) => throw new NotSupportedException();

    public Metadata GetMetadata(string topic, TimeSpan timeout) => CreateMetadata([_topics.Get(topic, _config)]);

    public Metadata GetMetadata(TimeSpan timeout) =>
        CreateMetadata(
            _topics.Where(topic => string.Equals(topic.BootstrapServers, _config.BootstrapServers, StringComparison.OrdinalIgnoreCase)));

    public Task CreatePartitionsAsync(
        IEnumerable<PartitionsSpecification> partitionsSpecifications,
        CreatePartitionsOptions? options = null) => throw new NotSupportedException();

    public Task DeleteGroupsAsync(IList<string> groups, DeleteGroupsOptions? options = null) =>
        throw new NotSupportedException();

    public Task DeleteTopicsAsync(IEnumerable<string> topics, DeleteTopicsOptions? options = null) =>
        throw new NotSupportedException();

    public Task CreateTopicsAsync(
        IEnumerable<TopicSpecification> topics,
        CreateTopicsOptions? options = null)
    {
        if (options is { ValidateOnly: true })
            throw new NotSupportedException("The ValidateOnly option is not supported by the mocked admin client.");

        List<CreateTopicReport> reports = [.. topics.Select(CreateTopic)];

        return reports.Exists(report => report.Error.IsError)
            ? Task.FromException(new CreateTopicsException(reports))
            : Task.CompletedTask;
    }

    public Task AlterConfigsAsync(
        Dictionary<ConfigResource, List<ConfigEntry>> configs,
        AlterConfigsOptions? options = null) => throw new NotSupportedException();

    public Task<List<IncrementalAlterConfigsResult>> IncrementalAlterConfigsAsync(
        Dictionary<ConfigResource, List<ConfigEntry>> configs,
        IncrementalAlterConfigsOptions? options = null) => throw new NotSupportedException();

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

    public Task<DeleteConsumerGroupOffsetsResult> DeleteConsumerGroupOffsetsAsync(
        string group,
        IEnumerable<TopicPartition> partitions,
        DeleteConsumerGroupOffsetsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<List<AlterConsumerGroupOffsetsResult>> AlterConsumerGroupOffsetsAsync(
        IEnumerable<ConsumerGroupTopicPartitionOffsets> groupPartitions,
        AlterConsumerGroupOffsetsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<List<ListConsumerGroupOffsetsResult>> ListConsumerGroupOffsetsAsync(
        IEnumerable<ConsumerGroupTopicPartitions> groupPartitions,
        ListConsumerGroupOffsetsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<ListConsumerGroupsResult> ListConsumerGroupsAsync(ListConsumerGroupsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<DescribeConsumerGroupsResult> DescribeConsumerGroupsAsync(IEnumerable<string> groups, DescribeConsumerGroupsOptions? options = null) =>
        throw new NotSupportedException();

    public Task<DescribeUserScramCredentialsResult> DescribeUserScramCredentialsAsync(
        IEnumerable<string> users,
        DescribeUserScramCredentialsOptions? options = null) =>
        throw new NotSupportedException();

    public Task AlterUserScramCredentialsAsync(
        IEnumerable<UserScramCredentialAlteration> alterations,
        AlterUserScramCredentialsOptions? options = null) =>
        throw new NotSupportedException();

    public void SetSaslCredentials(string username, string password) =>
        throw new NotSupportedException();

    public void Dispose()
    {
        // Nothing to dispose, it's just a mock
    }

    private static Metadata CreateMetadata(IEnumerable<IInMemoryTopic> topics) =>
        new(
            [new BrokerMetadata(0, "test", 42)],
            [.. topics.Select(CreateTopicMetadata)],
            0,
            "test");

    private static TopicMetadata CreateTopicMetadata(IInMemoryTopic topic) =>
        new(
            topic.Name,
            [
                .. Enumerable.Range(0, topic.Partitions.Count)
                    .Select(partition => new PartitionMetadata(
                        partition,
                        0,
                        [0],
                        [0],
                        NoError))
            ],
            NoError);

    private CreateTopicReport CreateTopic(TopicSpecification specification)
    {
        int? partitions = specification.ReplicasAssignments?.Count ?? (specification.NumPartitions > 0 ? specification.NumPartitions : null);
        bool isCreated = _topics.TryCreate(specification.Name, _config.BootstrapServers, partitions);

        return new CreateTopicReport
        {
            Topic = specification.Name,
            Error = new Error(isCreated ? ErrorCode.NoError : ErrorCode.TopicAlreadyExists)
        };
    }
}
