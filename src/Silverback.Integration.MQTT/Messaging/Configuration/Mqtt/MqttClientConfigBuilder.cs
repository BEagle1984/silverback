// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Formatter;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <inheritdoc cref="IMqttClientConfigBuilder" />
    public class MqttClientConfigBuilder : IMqttClientConfigBuilder
    {
        private readonly IServiceProvider? _serviceProvider;

        private readonly MqttClientOptionsBuilder _builder =
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500);

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttClientConfigBuilder" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required types (e.g. the
        ///     <see cref="IMqttExtendedAuthenticationExchangeHandler" />).
        /// </param>
        public MqttClientConfigBuilder(IServiceProvider? serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttClientConfigBuilder" /> class.
        /// </summary>
        /// <param name="baseConfig">
        ///     The <see cref="MqttClientConfig" /> to be used to initialize the builder.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required types (e.g. the
        ///     <see cref="IMqttExtendedAuthenticationExchangeHandler" />).
        /// </param>
        public MqttClientConfigBuilder(MqttClientConfig baseConfig, IServiceProvider? serviceProvider = null)
            : this(serviceProvider)
        {
            Check.NotNull(baseConfig, nameof(baseConfig));

            WithAuthentication(baseConfig.AuthenticationMethod, baseConfig.AuthenticationData);

            if (baseConfig.CleanSession)
                RequestCleanSession();
            else
                RequestPersistentSession();

            WithClientId(baseConfig.ClientId);

            if (baseConfig.ChannelOptions is MqttClientTcpOptions tcpOptions)
            {
                ConnectViaTcp(
                    options =>
                    {
                        options.Port = tcpOptions.Port;
                        options.Server = tcpOptions.Server;
                        options.AddressFamily = tcpOptions.AddressFamily;
                        options.BufferSize = tcpOptions.BufferSize;
                        options.DualMode = tcpOptions.DualMode;
                        options.NoDelay = tcpOptions.NoDelay;
                        options.TlsOptions = tcpOptions.TlsOptions;
                    });
            }
            else if (baseConfig.ChannelOptions is MqttClientWebSocketOptions webSocketOptions)
            {
                ConnectViaWebSocket(
                    options =>
                    {
                        options.Uri = webSocketOptions.Uri;
                        options.CookieContainer = webSocketOptions.CookieContainer;
                        options.ProxyOptions = webSocketOptions.ProxyOptions;
                        options.RequestHeaders = webSocketOptions.RequestHeaders;
                        options.SubProtocols = webSocketOptions.SubProtocols;
                        options.TlsOptions = webSocketOptions.TlsOptions;
                    });
            }

            if (baseConfig.Credentials != null)
                WithCredentials(baseConfig.Credentials);

            if (baseConfig.ExtendedAuthenticationExchangeHandler != null)
                UseExtendedAuthenticationExchangeHandler(baseConfig.ExtendedAuthenticationExchangeHandler);

            if (baseConfig.KeepAlivePeriod > TimeSpan.Zero)
                SendKeepAlive(baseConfig.KeepAlivePeriod);
            else
                DisableKeepAlive();

            LimitPacketSize(baseConfig.MaximumPacketSize);
            UseProtocolVersion(baseConfig.ProtocolVersion);
            LimitUnacknowledgedPublications(baseConfig.ReceiveMaximum);

            if (baseConfig.RequestProblemInformation)
                RequestProblemInformation();
            else
                DisableProblemInformation();

            if (baseConfig.RequestResponseInformation)
                RequestResponseInformation();
            else
                DisableResponseInformation();

            _builder.WithSessionExpiryInterval(baseConfig.SessionExpiryInterval);
            WithTimeout(baseConfig.Timeout);
            LimitTopicAlias(baseConfig.TopicAliasMaximum);

            if (baseConfig.TryPrivate)
                WithTryPrivate();
            else
                WithoutTryPrivate();

            baseConfig.UserProperties.ForEach(property => AddUserProperty(property.Name, property.Value));

            _builder.WithWillDelayInterval(baseConfig.WillDelayInterval);
            _builder.WithWillPayload(baseConfig.WillPayload);
            _builder.WithWillQualityOfServiceLevel(baseConfig.WillQualityOfServiceLevel);
            _builder.WithWillTopic(baseConfig.WillTopic);
            _builder.WithWillRetain(baseConfig.WillRetain);
            _builder.WithWillContentType(baseConfig.WillContentType);
            _builder.WithWillCorrelationData(baseConfig.WillCorrelationData);
            _builder.WithWillResponseTopic(baseConfig.WillResponseTopic);
            _builder.WithWillPayloadFormatIndicator(baseConfig.WillPayloadFormatIndicator);
            baseConfig.WillUserProperties.ForEach(property => _builder.WithWillUserProperty(property.Name, property.Value));
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithAuthentication" />
        public IMqttClientConfigBuilder WithAuthentication(string? method, byte[]? data)
        {
            _builder.WithAuthentication(method, data);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.RequestCleanSession" />
        public IMqttClientConfigBuilder RequestCleanSession()
        {
            _builder.WithCleanSession();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.RequestPersistentSession" />
        public IMqttClientConfigBuilder RequestPersistentSession()
        {
            _builder.WithCleanSession(false);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithClientId" />
        public IMqttClientConfigBuilder WithClientId(string value)
        {
            Check.NotEmpty(value, nameof(value));

            _builder.WithClientId(value);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectTo(Uri)" />
        public IMqttClientConfigBuilder ConnectTo(Uri uri)
        {
            Check.NotNull(uri, nameof(uri));

            _builder.WithConnectionUri(uri);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectTo(string)" />
        [SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Reviewed")]
        public IMqttClientConfigBuilder ConnectTo(string uri)
        {
            Check.NotEmpty(uri, nameof(uri));

            _builder.WithConnectionUri(uri);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithCredentials(string,string?)" />
        public IMqttClientConfigBuilder WithCredentials(string username, string? password = null)
        {
            Check.NotNull(username, nameof(username));

            _builder.WithCredentials(username, password);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithCredentials(string,byte[])" />
        public IMqttClientConfigBuilder WithCredentials(string username, byte[]? password = null)
        {
            Check.NotNull(username, nameof(username));

            _builder.WithCredentials(username, password);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithCredentials(IMqttClientCredentialsProvider)" />
        public IMqttClientConfigBuilder WithCredentials(IMqttClientCredentialsProvider credentialsProvider)
        {
            Check.NotNull(credentialsProvider, nameof(credentialsProvider));

            _builder.WithCredentials(credentialsProvider);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler)" />
        public IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler handler)
        {
            Check.NotNull(handler, nameof(handler));

            _builder.WithExtendedAuthenticationExchangeHandler(handler);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseExtendedAuthenticationExchangeHandler{THandler}" />
        public IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler<THandler>()
            where THandler : IMqttExtendedAuthenticationExchangeHandler =>
            UseExtendedAuthenticationExchangeHandler(typeof(THandler));

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseExtendedAuthenticationExchangeHandler(Type)" />
        public IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler(Type handlerType)
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("The service provider is not set.");

            _builder.WithExtendedAuthenticationExchangeHandler((IMqttExtendedAuthenticationExchangeHandler)_serviceProvider.GetRequiredService(handlerType));
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.SendKeepAlive" />
        public IMqttClientConfigBuilder SendKeepAlive(TimeSpan interval)
        {
            Check.Range(interval, nameof(interval), TimeSpan.Zero, TimeSpan.MaxValue);

            _builder.WithKeepAlivePeriod(interval);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.DisableKeepAlive" />
        public IMqttClientConfigBuilder DisableKeepAlive()
        {
            _builder.WithNoKeepAlive();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.LimitPacketSize" />
        public IMqttClientConfigBuilder LimitPacketSize(uint maximumPacketSize)
        {
            _builder.WithMaximumPacketSize(maximumPacketSize);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseProtocolVersion" />
        public IMqttClientConfigBuilder UseProtocolVersion(MqttProtocolVersion value)
        {
            _builder.WithProtocolVersion(value);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseProxy(string,string?,string?,string?,bool,string[])" />
        public IMqttClientConfigBuilder UseProxy(
            string address,
            string? username = null,
            string? password = null,
            string? domain = null,
            bool bypassOnLocal = false,
            string[]? bypassList = null)
        {
            Check.NotNull(address, nameof(address));

            _builder.WithProxy(address, username, password, domain, bypassOnLocal, bypassList);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.UseProxy(Action{MqttClientWebSocketProxyOptions})" />
        public IMqttClientConfigBuilder UseProxy(Action<MqttClientWebSocketProxyOptions> optionsAction)
        {
            Check.NotNull(optionsAction, nameof(optionsAction));

            _builder.WithProxy(optionsAction);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.LimitUnacknowledgedPublications" />
        public IMqttClientConfigBuilder LimitUnacknowledgedPublications(ushort receiveMaximum)
        {
            _builder.WithReceiveMaximum((ushort)receiveMaximum);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.RequestProblemInformation" />
        public IMqttClientConfigBuilder RequestProblemInformation()
        {
            _builder.WithRequestProblemInformation();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.DisableProblemInformation" />
        public IMqttClientConfigBuilder DisableProblemInformation()
        {
            _builder.WithRequestProblemInformation(false);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.RequestResponseInformation" />
        public IMqttClientConfigBuilder RequestResponseInformation()
        {
            _builder.WithRequestResponseInformation();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.DisableResponseInformation" />
        public IMqttClientConfigBuilder DisableResponseInformation()
        {
            _builder.WithRequestResponseInformation(false);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithSessionExpiration" />
        public IMqttClientConfigBuilder WithSessionExpiration(TimeSpan sessionExpiryInterval)
        {
            Check.Range(
                sessionExpiryInterval,
                nameof(sessionExpiryInterval),
                TimeSpan.Zero,
                TimeSpan.MaxValue);

            _builder.WithSessionExpiryInterval((uint)sessionExpiryInterval.TotalSeconds);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectViaTcp(string,int?)" />
        public IMqttClientConfigBuilder ConnectViaTcp(string server, int? port = null)
        {
            Check.NotNull(server, nameof(server));

            _builder.WithTcpServer(server, port);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectViaTcp(Action{MqttClientTcpOptions})" />
        public IMqttClientConfigBuilder ConnectViaTcp(Action<MqttClientTcpOptions> optionsAction)
        {
            Check.NotNull(optionsAction, nameof(optionsAction));

            _builder.WithTcpServer(optionsAction);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithTimeout" />
        public IMqttClientConfigBuilder WithTimeout(TimeSpan timeout)
        {
            Check.Range(timeout, nameof(timeout), TimeSpan.Zero, TimeSpan.MaxValue);

            _builder.WithTimeout(timeout);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.DisableTls" />
        public IMqttClientConfigBuilder DisableTls()
        {
            _builder.WithTls(
                parameters =>
                {
                    parameters.UseTls = false;
                });
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.EnableTls()" />
        public IMqttClientConfigBuilder EnableTls()
        {
            _builder.WithTls();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.EnableTls(MqttClientOptionsBuilderTlsParameters)" />
        public IMqttClientConfigBuilder EnableTls(MqttClientOptionsBuilderTlsParameters parameters)
        {
            Check.NotNull(parameters, nameof(parameters));

            _builder.WithTls(parameters);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.EnableTls(Action{MqttClientOptionsBuilderTlsParameters})" />
        public IMqttClientConfigBuilder EnableTls(Action<MqttClientOptionsBuilderTlsParameters> parametersAction)
        {
            Check.NotNull(parametersAction, nameof(parametersAction));

            var parameters = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true
            };

            parametersAction.Invoke(parameters);

            _builder.WithTls(parameters);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.LimitTopicAlias" />
        public IMqttClientConfigBuilder LimitTopicAlias(int topicAliasMaximum)
        {
            Check.Range(topicAliasMaximum, nameof(topicAliasMaximum), 0, ushort.MaxValue);

            _builder.WithTopicAliasMaximum((ushort)topicAliasMaximum);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithTryPrivate" />
        public IMqttClientConfigBuilder WithTryPrivate()
        {
            _builder.WithTryPrivate();
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.WithoutTryPrivate" />
        public IMqttClientConfigBuilder WithoutTryPrivate()
        {
            _builder.WithTryPrivate(false);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.AddUserProperty" />
        public IMqttClientConfigBuilder AddUserProperty(string name, string? value)
        {
            Check.NotNull(name, nameof(name));

            _builder.WithUserProperty(name, value);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectViaWebSocket(string,Action{MqttClientOptionsBuilderWebSocketParameters})" />
        [SuppressMessage("", "CA1054", Justification = "Uri declared as string in underlying lib")]
        public IMqttClientConfigBuilder ConnectViaWebSocket(
            string uri,
            Action<MqttClientOptionsBuilderWebSocketParameters> parametersAction)
        {
            Check.NotNull(uri, nameof(uri));
            Check.NotNull(parametersAction, nameof(parametersAction));

            var parameters = new MqttClientOptionsBuilderWebSocketParameters();
            parametersAction.Invoke(parameters);

            _builder.WithWebSocketServer(uri, parameters);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectViaWebSocket(string,Action{MqttClientOptionsBuilderWebSocketParameters})" />
        [SuppressMessage("", "CA1054", Justification = "Uri declared as string in underlying lib")]
        public IMqttClientConfigBuilder ConnectViaWebSocket(
            string uri,
            MqttClientOptionsBuilderWebSocketParameters? parameters = null)
        {
            Check.NotNull(uri, nameof(uri));

            _builder.WithWebSocketServer(uri, parameters);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.ConnectViaWebSocket(Action{MqttClientWebSocketOptions})" />
        public IMqttClientConfigBuilder ConnectViaWebSocket(Action<MqttClientWebSocketOptions> optionsAction)
        {
            Check.NotNull(optionsAction, nameof(optionsAction));

            _builder.WithWebSocketServer(optionsAction);
            return this;
        }

        /// <inheritdoc cref="IMqttClientConfigBuilder.SendLastWillMessage" />
        public IMqttClientConfigBuilder SendLastWillMessage(Action<IMqttLastWillMessageBuilder> lastWillBuilderAction)
        {
            Check.NotNull(lastWillBuilderAction, nameof(lastWillBuilderAction));

            var builder = new MqttLastWillMessageBuilder();
            lastWillBuilderAction.Invoke(builder);
            builder.Build(_builder);

            return this;
        }

        /// <summary>
        ///     Builds the <see cref="MqttClientConfig" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="MqttClientConfig" />.
        /// </returns>
        public MqttClientConfig Build() => new(_builder.Build());
    }
}
