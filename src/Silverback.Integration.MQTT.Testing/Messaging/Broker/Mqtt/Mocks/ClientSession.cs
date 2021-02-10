// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    internal sealed class ClientSession : IDisposable, IClientSession
    {
        private readonly IMqttApplicationMessageReceivedHandler _messageHandler;

        private readonly Channel<MqttApplicationMessage> _channel =
            Channel.CreateUnbounded<MqttApplicationMessage>();

        private readonly List<Subscription> _subscriptions = new();

        private CancellationTokenSource _readCancellationTokenSource = new();

        private int _pendingMessagesCount;

        public ClientSession(
            IMqttClientOptions clientOptions,
            IMqttApplicationMessageReceivedHandler messageHandler)
        {
            ClientOptions = Check.NotNull(clientOptions, nameof(clientOptions));
            _messageHandler = Check.NotNull(messageHandler, nameof(messageHandler));
        }

        public IMqttClientOptions ClientOptions { get; }

        public int PendingMessagesCount => _pendingMessagesCount;

        public bool IsConnected { get; private set; }

        public bool IsConsumerDisconnected =>
            !((_messageHandler as MockedMqttClient)?.Consumer?.IsConnected ?? true);

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public void Connect()
        {
            if (IsConnected)
                return;

            IsConnected = true;

            if (_readCancellationTokenSource.IsCancellationRequested)
                _readCancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => ReadChannelAsync(_readCancellationTokenSource.Token));
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            IsConnected = false;

            _readCancellationTokenSource.Cancel();
        }

        public void Subscribe(IReadOnlyCollection<string> topics)
        {
            lock (_subscriptions)
            {
                foreach (var topic in topics)
                {
                    if (_subscriptions.Any(subscription => subscription.Topic == topic))
                        continue;

                    _subscriptions.Add(new Subscription(topic, GetSubscriptionRegex(topic)));
                }
            }
        }

        public void Unsubscribe(IReadOnlyCollection<string> topics)
        {
            lock (_subscriptions)
            {
                _subscriptions.RemoveAll(subscription => topics.Contains(subscription.Topic));
            }
        }

        public async ValueTask PushAsync(MqttApplicationMessage message)
        {
            lock (_subscriptions)
            {
                if (_subscriptions.All(subscription => !subscription.Regex.IsMatch(message.Topic)))
                    return;
            }

            await _channel.Writer.WriteAsync(message).ConfigureAwait(false);

            Interlocked.Increment(ref _pendingMessagesCount);
        }

        public void Dispose()
        {
            Disconnect();

            _readCancellationTokenSource.Dispose();
        }

        private static Regex GetSubscriptionRegex(string topic)
        {
            var pattern = Regex.Escape(topic)
                .Replace("\\+", "[\\w]*", StringComparison.Ordinal)
                .Replace("\\#", "[\\w\\/]*", StringComparison.Ordinal);

            return new Regex($"^{pattern}$", RegexOptions.Compiled);
        }

        private async Task ReadChannelAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _channel.Reader.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);

                var eventArgs = new MqttApplicationMessageReceivedEventArgs(
                    ClientOptions.ClientId,
                    message);

                await _messageHandler.HandleApplicationMessageReceivedAsync(eventArgs)
                    .ConfigureAwait(false);

                Interlocked.Decrement(ref _pendingMessagesCount);
            }
        }

        // TODO: Convert to short record declaration as soon as the StyleCop.Analyzers is updated,
        //       see https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3181
        // private record Subscription(string Topic, Regex Regex);
        private record Subscription
        {
            public Subscription(string topic, Regex regex)
            {
                Topic = topic;
                Regex = regex;
            }

            public string Topic { get; }

            public Regex Regex { get; }
        }
    }
}
