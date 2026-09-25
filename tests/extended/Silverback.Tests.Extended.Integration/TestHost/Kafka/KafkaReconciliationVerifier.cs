// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Tests.Extended.Shared.Kafka;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

public sealed class KafkaReconciliationVerifier
{
    private readonly IReadOnlyDictionary<(int Partition, long Offset), ReconciliationMessage> _produced;

    private readonly Dictionary<int, long> _partitionEnds;

    private readonly HashSet<(int Partition, long Offset)> _seen = [];

    private readonly Dictionary<int, long> _firstSeen = [];

    private readonly HashSet<(string Member, int Epoch, int Partition)> _assigned = [];

    private readonly HashSet<(string Member, int Epoch, int Partition)> _revoked = [];

    private readonly Dictionary<(string Member, int Partition), (int Epoch, long ChannelId)> _activeChannels = [];

    private readonly HashSet<(string Member, long ChannelId)> _channels = [];

    private readonly Dictionary<(string Member, int Epoch, int Partition, long ChannelId), long> _channelStarts = [];

    private readonly Dictionary<(string Member, int Epoch, int Partition, long ChannelId), long> _lastInChannel = [];

    private readonly Dictionary<(string Member, int Epoch, int Partition), long> _lastInAssignment = [];

    private readonly List<string> _violations = [];

    private int _receipts;

    private int _sameAssignmentReplays;

    private int _commitObservations;

    public KafkaReconciliationVerifier(IReadOnlyDictionary<(int Partition, long Offset), ReconciliationMessage> produced)
    {
        _produced = produced;
        _partitionEnds = produced.Keys.GroupBy(key => key.Partition)
            .ToDictionary(group => group.Key, group => group.Max(key => key.Offset) + 1);
    }

    public void Observe(ProcessingReceipt receipt, int journalPartition)
    {
        if (receipt.Partition != journalPartition)
            _violations.Add($"Receipt on wrong partition: {receipt}");

        (string Member, int Epoch, int Partition) assignment = (receipt.Member, receipt.Epoch, receipt.Partition);

        switch (receipt.Kind)
        {
            case "assigned":
                if (!_assigned.Add(assignment))
                    _violations.Add($"Repeated assignment: {receipt}");

                break;
            case "revoked":
                if (!_assigned.Contains(assignment) || !_revoked.Add(assignment))
                    _violations.Add($"Revocation outside assignment: {receipt}");

                break;
            case "channel-started":
                VerifyAssignment(receipt);
                StartChannel(receipt);
                break;
            case "processed":
                VerifyAssignment(receipt);
                Process(receipt);
                break;
            case "committed":
                VerifyCommit(receipt);
                break;
            default:
                _violations.Add($"Unknown receipt kind: {receipt}");
                break;
        }
    }

    public ReconciliationReport GetReport()
    {
        List<string> violations = [.. _violations];

        foreach ((int partition, long offset) in _produced.Keys.Where(key => !_seen.Contains(key)))
        {
            violations.Add($"Missing record: {partition}@{offset}");
        }

        if (_assigned.Count <= _partitionEnds.Count)
            violations.Add("The test did not observe reassignment.");

        return new ReconciliationReport(
            _produced.Count,
            _seen.Count,
            _receipts - _seen.Count,
            _assigned.Count,
            _revoked.Count,
            _channels.Count,
            _sameAssignmentReplays,
            _commitObservations,
            violations);
    }

    private void VerifyAssignment(ProcessingReceipt receipt)
    {
        (string Member, int Epoch, int Partition) assignment = (receipt.Member, receipt.Epoch, receipt.Partition);

        if (!_assigned.Contains(assignment) || _revoked.Contains(assignment))
            _violations.Add($"Processing outside assignment: {receipt}");
    }

    private void StartChannel(ProcessingReceipt receipt)
    {
        (string Member, int Partition) partition = (receipt.Member, receipt.Partition);

        if (receipt.ChannelId <= 0 ||
            _activeChannels.TryGetValue(partition, out (int Epoch, long ChannelId) previous) && receipt.ChannelId <= previous.ChannelId)
        {
            _violations.Add($"Reused or obsolete processing channel: {receipt}");
        }

        _activeChannels[partition] = (receipt.Epoch, receipt.ChannelId);
        _channels.Add((receipt.Member, receipt.ChannelId));
        _channelStarts[(receipt.Member, receipt.Epoch, receipt.Partition, receipt.ChannelId)] = receipt.Offset;
    }

    private void Process(ProcessingReceipt receipt)
    {
        _receipts++;
        (int Partition, long Offset) key = (receipt.Partition, receipt.Offset);
        (string Member, int Epoch, int Partition) assignment = (receipt.Member, receipt.Epoch, receipt.Partition);
        (string Member, int Epoch, int Partition, long ChannelId) channel = (receipt.Member, receipt.Epoch, receipt.Partition, receipt.ChannelId);

        if (receipt.ChannelId <= 0 ||
            !_activeChannels.TryGetValue((receipt.Member, receipt.Partition), out (int Epoch, long ChannelId) active) ||
            active != (receipt.Epoch, receipt.ChannelId))
        {
            _violations.Add($"Processing outside active channel: {receipt}");
        }

        if (!_produced.TryGetValue(key, out ReconciliationMessage? expected) || expected.Sequence != receipt.Sequence)
            _violations.Add($"Unknown or mismatched record: {receipt}");

        if (_lastInChannel.TryGetValue(channel, out long previous))
        {
            if (receipt.Offset <= previous)
                _violations.Add($"Non-increasing offset within processing channel: {receipt}; previous={previous}");
        }
        else if (!_channelStarts.TryGetValue(channel, out long start) || receipt.Offset != start)
        {
            _violations.Add($"Processing did not begin at the announced channel offset: {receipt}");
        }

        _lastInChannel[channel] = receipt.Offset;

        if (_lastInAssignment.TryGetValue(assignment, out long previousInAssignment) && receipt.Offset <= previousInAssignment)
            _sameAssignmentReplays++;

        _lastInAssignment[assignment] = Math.Max(receipt.Offset, _lastInAssignment.GetValueOrDefault(assignment, -1));

        if (_seen.Add(key))
        {
            if (_firstSeen.TryGetValue(receipt.Partition, out long first) && receipt.Offset <= first)
                _violations.Add($"Out-of-order first processing: {receipt}; previous={first}");

            _firstSeen[receipt.Partition] = Math.Max(receipt.Offset, _firstSeen.GetValueOrDefault(receipt.Partition, -1));
        }
    }

    private void VerifyCommit(ProcessingReceipt receipt)
    {
        _commitObservations++;

        if (!_partitionEnds.TryGetValue(receipt.Partition, out long end) || receipt.Offset < 0 || receipt.Offset > end)
        {
            _violations.Add($"Commit outside produced range: {receipt}");

            return;
        }

        if (_produced.Keys.Any(key => key.Partition == receipt.Partition && key.Offset < receipt.Offset && !_seen.Contains(key)))
            _violations.Add($"Commit crossed an unfinished record: {receipt}");
    }
}
