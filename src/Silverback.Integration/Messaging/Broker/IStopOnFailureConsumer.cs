// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Stops the consumer because the processing of a message failed, reporting whether the stop was accepted, unlike
///     <see cref="IConsumer.StopAsync(IBrokerMessageIdentifier, bool)" />.
/// </summary>
internal interface IStopOnFailureConsumer
{
    /// <summary>
    ///     Requests the consumer to stop because the processing of the specified message failed, without waiting for the processing to
    ///     complete. The request is ignored if the processing is stale (see <see cref="IConsumer.StopAsync(IBrokerMessageIdentifier, bool)" />)
    ///     or was canceled by a stop, also when the consumer was restarted in the meantime.
    /// </summary>
    /// <param name="brokerMessageIdentifier">
    ///     The original identifier from the received message that caused the stop request.
    /// </param>
    /// <param name="processingCancellationToken">
    ///     The cancellation token passed to the pipeline processing the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the start generation of
    ///     the stopped consumer (see <see cref="DisconnectAfterStopOnFailureAsync" />), or <c>null</c> if the request was ignored.
    /// </returns>
    ValueTask<int?> TryStopOnFailureAsync(IBrokerMessageIdentifier brokerMessageIdentifier, CancellationToken processingCancellationToken);

    /// <summary>
    ///     Disconnects the client after the consumer was stopped because the processing of a message failed (see
    ///     <see cref="TryStopOnFailureAsync" />), unless the consumer was restarted in the meantime.
    /// </summary>
    /// <param name="startGeneration">
    ///     The start generation of the stopped consumer, returned by <see cref="TryStopOnFailureAsync" />.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask DisconnectAfterStopOnFailureAsync(int startGeneration);
}
