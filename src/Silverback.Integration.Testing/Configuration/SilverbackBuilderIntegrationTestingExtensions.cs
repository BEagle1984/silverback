// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>AddIntegrationSpy</c> and <c>AddIntegrationSpyAndSubscriber</c> methods to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderIntegrationTestingExtensions
{
    /// <summary>
    ///     Adds the <see cref="IIntegrationSpy" /> and its support services to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="silverbackBuilder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="attachSubscriber">
    ///     Specifies whether a generic subscriber (<see cref="InboundSpySubscriber" /> must be used to monitor the
    ///     inbound messages instead of a behavior (<see cref="InboundSpyBrokerBehavior" />).
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddIntegrationSpy(this SilverbackBuilder silverbackBuilder, bool attachSubscriber = false)
    {
        Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

        silverbackBuilder
            .AddSingletonBrokerBehavior<OutboundSpyBrokerBehavior>()
            .AddSingletonBrokerBehavior<RawOutboundSpyBrokerBehavior>()
            .AddSingletonBrokerBehavior<RawInboundSpyBrokerBehavior>();

        if (attachSubscriber)
            silverbackBuilder.AddSingletonSubscriber<InboundSpySubscriber>();
        else
            silverbackBuilder.AddSingletonBrokerBehavior<InboundSpyBrokerBehavior>();

        silverbackBuilder.Services
            .AddSingleton<IIntegrationSpy>(serviceProvider => serviceProvider.GetRequiredService<IntegrationSpy>())
            .AddSingleton<IntegrationSpy>();

        return silverbackBuilder;
    }

    /// <summary>
    ///     Adds the <see cref="IIntegrationSpy" /> and its support services to the
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> and uses a generic
    ///     subscriber to monitor the incoming messages. This is the same as calling <see cref="AddIntegrationSpy" />
    ///     with the <c>attachSubscriber</c> parameter set to <c>true</c>.
    /// </summary>
    /// <param name="silverbackBuilder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
    ///     the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddIntegrationSpyAndSubscriber(this SilverbackBuilder silverbackBuilder) =>
        silverbackBuilder.AddIntegrationSpy(true);
}
