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
    ///     Adds the <c>AddSequenceWriter</c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddSequenceWriterExtensions
    {
        /// <summary>
        ///     Adds a transient sequence writer of the type specified in <paramref name="writerType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="writerType">
        ///     The type of the writer to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSequenceWriter(
            this ISilverbackBuilder silverbackBuilder,
            Type writerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransient(typeof(ISequenceWriter), writerType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a transient sequence writer of the type specified in <typeparamref name="TWriter" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TWriter">
        ///     The type of the writer to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSequenceWriter<TWriter>(
            this ISilverbackBuilder silverbackBuilder)
            where TWriter : class, ISequenceWriter =>
            AddTransientSequenceWriter(silverbackBuilder, typeof(TWriter));

        /// <summary>
        ///     Adds a transient sequence writer with a factory specified in <paramref name="implementationFactory" /> to
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
        public static ISilverbackBuilder AddTransientSequenceWriter(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, ISequenceWriter> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransient(typeof(ISequenceWriter), implementationFactory);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence writer of the type specified in <paramref name="writerType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="writerType">
        ///     The type of the writer to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceWriter(
            this ISilverbackBuilder silverbackBuilder,
            Type writerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceWriter), writerType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence writer of the type specified in <typeparamref name="TWriter" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TWriter">
        ///     The type of the writer to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSequenceWriter<TWriter>(
            this ISilverbackBuilder silverbackBuilder)
            where TWriter : class, ISequenceWriter =>
            AddSingletonSequenceWriter(silverbackBuilder, typeof(TWriter));

        /// <summary>
        ///     Adds a singleton sequence writer with a factory specified in <paramref name="implementationFactory" /> to
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
        public static ISilverbackBuilder AddSingletonSequenceWriter(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, ISequenceWriter> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceWriter), implementationFactory);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton sequence writer with an instance specified in <paramref name="implementationInstance" />
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
        public static ISilverbackBuilder AddSingletonSequenceWriter(
            this ISilverbackBuilder silverbackBuilder,
            ISequenceWriter implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingleton(typeof(ISequenceWriter), implementationInstance);

            return silverbackBuilder;
        }
    }
}
