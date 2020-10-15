// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddSequenceReader</c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddSequenceReaderExtensions
    {
        /// <summary>
        ///     Adds a transient sequence reader of the type specified in <paramref name="readerType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="readerType">
        ///     The type of the reader to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSequenceReader(
            this ISilverbackBuilder silverbackBuilder,
            Type readerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransient(typeof(ISequenceReader), readerType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a transient sequence reader of the type specified in <typeparamref name="TReader" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TReader">
        ///     The type of the reader to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSequenceReader<TReader>(
            this ISilverbackBuilder silverbackBuilder)
            where TReader : class, ISequenceReader =>
            AddTransientSequenceReader(silverbackBuilder, typeof(TReader));

        /// <summary>
        ///     Adds a transient sequence reader with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSequenceReader(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, ISequenceReader> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransient(typeof(ISequenceReader), implementationFactory);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence reader of the type specified in <paramref name="readerType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="readerType">
        ///     The type of the reader to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceReader(
            this ISilverbackBuilder silverbackBuilder,
            Type readerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceReader), readerType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence reader of the type specified in <typeparamref name="TReader" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TReader">
        ///     The type of the reader to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceReader<TReader>(
            this ISilverbackBuilder silverbackBuilder)
            where TReader : class, ISequenceReader =>
            AddSingletonSequenceReader(silverbackBuilder, typeof(TReader));

        /// <summary>
        ///     Adds a singleton sequence reader with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceReader(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, ISequenceReader> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceReader), implementationFactory);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence reader with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceReader(
            this ISilverbackBuilder silverbackBuilder,
            ISequenceReader implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceReader), implementationInstance);

            return silverbackBuilder;
        }
    }
}
