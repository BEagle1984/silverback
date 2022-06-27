// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    internal sealed class MessageStreamProvider<TMessage> : IMessageStreamProvider, IDisposable
    {
        private readonly List<ILazyMessageStreamEnumerable> _lazyStreams = new();

        private int _messagesCount;

        private MethodInfo? _genericCreateStreamMethodInfo;

        private bool _completed;

        private bool _aborted;

        /// <inheritdoc cref="IMessageStreamProvider.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="IMessageStreamProvider.StreamsCount" />
        public int StreamsCount => _lazyStreams.Count;

        /// <summary>
        ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the
        ///     message has actually been pulled and processed.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task will complete only
        ///     when the message has actually been pulled and processed and its result contains the number of
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> that have been pushed.
        /// </returns>
        public Task<int> PushAsync(TMessage message, CancellationToken cancellationToken = default) =>
            PushAsync(message, true, cancellationToken);

        /// <summary>
        ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the
        ///     message has actually been pulled and processed.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task will complete only
        ///     when the message has actually been pulled and processed and its result contains the number of
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> that have been pushed.
        /// </returns>
        [SuppressMessage(
            "ReSharper",
            "ParameterOnlyUsedForPreconditionCheck.Global",
            Justification = "False positive")]
        public async Task<int> PushAsync(
            TMessage message,
            bool throwIfUnhandled,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull<object>(message, nameof(message));

            if (_completed || _aborted)
                throw new InvalidOperationException("The streams are already completed or aborted.");

            var messageId = Interlocked.Increment(ref _messagesCount);

            var processingTasks = PushToCompatibleStreams(messageId, message, cancellationToken).ToList();

            if (processingTasks.Count > 0)
                await Task.WhenAll(processingTasks).ConfigureAwait(false);
            else if (throwIfUnhandled)
                throw new UnhandledMessageException(message!);

            return processingTasks.Count;
        }

        /// <inheritdoc cref="Abort"/>
        /// <remarks>
        ///     The abort is performed only if the streams haven't been completed already.
        /// </remarks>
        public void AbortIfPending()
        {
            if (!_completed)
                Abort();
        }

        /// <summary>
        ///     Aborts the ongoing enumerations and the pending calls to
        ///     <see cref="PushAsync(TMessage,CancellationToken)" />, then marks the
        ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be
        ///     thrown by the enumerators and the <see cref="PushAsync(TMessage,CancellationToken)" /> method.
        /// </summary>
        public void Abort()
        {
            if (_completed)
                throw new InvalidOperationException("The streams are already completed.");

            if (_aborted)
                return;

            _lazyStreams.ParallelForEach(
                lazyStream =>
                {
                    if (lazyStream.Stream != null)
                        lazyStream.Stream.Abort();
                    else
                        lazyStream.Cancel();
                });

            _aborted = true;
        }

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (_aborted)
                throw new InvalidOperationException("The stream are already aborted.");

            if (_completed)
                return;

            await _lazyStreams.ParallelForEachAsync(
                async lazyStream =>
                {
                    if (lazyStream.Stream != null)
                        await lazyStream.Stream.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    else
                        lazyStream.Cancel();
                }).ConfigureAwait(false);

            _completed = true;
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream" />
        public IMessageStreamEnumerable<object> CreateStream(
            Type messageType,
            IReadOnlyCollection<IMessageFilter>? filters = null)
        {
            var lazyStream = (ILazyMessageStreamEnumerable)CreateLazyStream(messageType, filters);
            return (IMessageStreamEnumerable<object>)lazyStream.GetOrCreateStream();
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateStream{TMessage}" />
        public IMessageStreamEnumerable<TMessageLinked> CreateStream<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null)
        {
            var lazyStream = (ILazyMessageStreamEnumerable)CreateLazyStream<TMessageLinked>(filters);
            return (IMessageStreamEnumerable<TMessageLinked>)lazyStream.GetOrCreateStream();
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateLazyStream" />
        public ILazyMessageStreamEnumerable<object> CreateLazyStream(
            Type messageType,
            IReadOnlyCollection<IMessageFilter>? filters = null)
        {
            _genericCreateStreamMethodInfo ??= GetType().GetMethod(
                nameof(CreateLazyStream),
                1,
                new[] { typeof(IReadOnlyCollection<IMessageFilter>) });

            object lazyStream = _genericCreateStreamMethodInfo
                .MakeGenericMethod(messageType)
                .Invoke(this, new object?[] { filters });

            return (ILazyMessageStreamEnumerable<object>)lazyStream;
        }

        /// <inheritdoc cref="IMessageStreamProvider.CreateLazyStream{TMessage}" />
        public ILazyMessageStreamEnumerable<TMessageLinked> CreateLazyStream<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null)
        {
            var stream = CreateLazyStreamCore<TMessageLinked>(filters);

            lock (_lazyStreams)
            {
                _lazyStreams.Add((ILazyMessageStreamEnumerable)stream);
            }

            return stream;
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            if (!_aborted && !_completed)
                AsyncHelper.RunSynchronously(() => CompleteAsync());
        }

        private static ILazyMessageStreamEnumerable<TMessageLinked> CreateLazyStreamCore<TMessageLinked>(IReadOnlyCollection<IMessageFilter>? filters = null) =>
            new LazyMessageStreamEnumerable<TMessageLinked>(filters);

        private static bool PushIfCompatibleType(
            ILazyMessageStreamEnumerable lazyStream,
            int messageId,
            TMessage message,
            CancellationToken cancellationToken,
            out Task messageProcessingTask)
        {
            if (message == null || IsFiltered(lazyStream.Filters, message))
            {
                messageProcessingTask = Task.CompletedTask;
                return false;
            }

            if (lazyStream.MessageType.IsInstanceOfType(message))
            {
                var pushedMessage = new PushedMessage(messageId, message, message);
                messageProcessingTask =
                    lazyStream.GetOrCreateStream().PushAsync(pushedMessage, cancellationToken);
                return true;
            }

            var envelope = message as IEnvelope;
            if (envelope?.Message != null && envelope.AutoUnwrap &&
                lazyStream.MessageType.IsInstanceOfType(envelope.Message))
            {
                var pushedMessage = new PushedMessage(messageId, envelope.Message, message);
                messageProcessingTask =
                    lazyStream.GetOrCreateStream().PushAsync(pushedMessage, cancellationToken);
                return true;
            }

            messageProcessingTask = Task.CompletedTask;
            return false;
        }

        private static bool IsFiltered(IReadOnlyCollection<IMessageFilter>? filters, object message) =>
            filters != null && filters.Count != 0 && !filters.All(filter => filter.MustProcess(message));

        private IEnumerable<Task> PushToCompatibleStreams(
            int messageId,
            TMessage message,
            CancellationToken cancellationToken)
        {
            foreach (var lazyStream in _lazyStreams)
            {
                if (PushIfCompatibleType(
                        lazyStream,
                        messageId,
                        message,
                        cancellationToken,
                        out var processingTask))
                {
                    yield return processingTask;
                }
            }
        }
    }
}
