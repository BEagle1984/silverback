using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds methods to register <see cref="IBrokerEventsHandler"/>s to the <see cref="ISilverbackBuilder"/>.
    /// </summary>
    public static class SilverbackBuilderAddBrokerEventsHandlerExtensions
    {
        /// <summary>
        ///     Adds a singleton <see cref="IBrokerEventsHandler"/> of the type specified in
        ///     <typeparamref name="THandler"/> to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder"/>.
        /// </param>
        /// <typeparam name="THandler">
        ///     The type of the <see cref="IBrokerEventsHandler"/> to register.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerEventsHandler<THandler>(
            this ISilverbackBuilder builder)
            where THandler : class, IBrokerEventsHandler
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddSingleton<IBrokerEventsHandler, THandler>();

            return builder;
        }

        /// <summary>
        ///     Adds a scoped <see cref="IBrokerEventsHandler"/> of the type specified in
        ///     <typeparamref name="THandler"/> to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder"/>.
        /// </param>
        /// <typeparam name="THandler">
        ///     The type of the <see cref="IBrokerEventsHandler"/> to register.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBrokerEventsHandler<THandler>(
            this ISilverbackBuilder builder)
            where THandler : class, IBrokerEventsHandler
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped<IBrokerEventsHandler, THandler>();

            return builder;
        }

        /// <summary>
        ///     Adds a transient <see cref="IBrokerEventsHandler"/> of the type specified in
        ///     <typeparamref name="THandler"/> to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder"/>.
        /// </param>
        /// <typeparam name="THandler">
        ///     The type of the <see cref="IBrokerEventsHandler"/> to register.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBrokerEventsHandler<THandler>(
            this ISilverbackBuilder builder)
            where THandler : class, IBrokerEventsHandler
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddTransient<IBrokerEventsHandler, THandler>();

            return builder;
        }
    }
}
