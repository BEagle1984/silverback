// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Testing;

/// <content>
///     Implements the Kafka commit wait methods.
/// </content>
public partial class KafkaTestingHelper
{
    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(params string[] endpointNames) =>
        WaitUntilAllMessagesAreCommittedAsync(true, null, endpointNames);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(TimeSpan?,string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(TimeSpan? timeout, params string[] endpointNames) =>
        WaitUntilAllMessagesAreCommittedAsync(true, timeout, endpointNames);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, params string[] endpointNames) =>
        WaitUntilAllMessagesAreCommittedAsync(throwTimeoutException, null, endpointNames);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,TimeSpan?)" />
    public async ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout = null)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        await WaitUntilAllMessagesAreCommittedAsync(throwTimeoutException, cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,TimeSpan?,string[])" />
    public async ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout, params string[] endpointNames)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        await WaitUntilAllMessagesAreCommittedAsync(throwTimeoutException, cancellationTokenSource.Token, endpointNames).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(CancellationToken,string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(CancellationToken cancellationToken, params string[] endpointNames) =>
        WaitUntilAllMessagesAreCommittedAsync(true, cancellationToken, endpointNames);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,CancellationToken,string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(
        bool throwTimeoutException,
        CancellationToken cancellationToken,
        params string[] endpointNames) =>
        WaitUntilAllMessagesCoreAsync(
            WaitUntilAllMessagesAreCommittedCoreAsync,
            "Timeout elapsed before all message offsets could be committed",
            throwTimeoutException,
            endpointNames,
            cancellationToken);

    private Task WaitUntilAllMessagesAreCommittedCoreAsync(IReadOnlyCollection<string> endpointNames, CancellationToken cancellationToken)
    {
        if (_groups == null)
            return Task.CompletedTask;

        string[] topicNames = [.. endpointNames.Select(GetEndpointRawName)];

        return Task.WhenAll(
            _groups.Select(group => group.WaitUntilAllMessagesAreCommittedAsync(topicNames, cancellationToken).AsTask()));
    }
}
